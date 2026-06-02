# Payroll Module — Implementation Plan & Specification

> A generalized, configurable payroll module for the standard Vectus stack
> (.NET 10 / Blazor RCL + Dapper + stored procedures + SSDT). Designed to be
> portable to other LOB projects on the same stack.
>
> **Decisions locked in:**
> - Statutory (PF / ESI / PT / TDS) = **configurable, data-driven engine** with Indian defaults — nothing hardcoded.
> - Integration = **post salary journal to FinancialAccounting** + **manual attendance/LOP entry** per run.
> - Employees are **independent** of Fleet drivers.
> - Phased build. This document is **Phase 0: the spec**.

---

## 1. Design principles (why it's "generalized")

1. **Everything is a configured component.** Basic, HRA, DA, PF, ESI, PT, TDS, loans —
   all rows in a `SalaryComponent` master. Each declares *how* it is computed
   (Fixed / % of base / Slab / Statutory-rule) and *what it affects* (taxable, part of
   PF/ESI base, earning vs deduction). Adding a new pay head = a data row, not code.
2. **Statutory rules are versioned data, not constants.** A `StatutoryRule` is the stable identity
   (PF-EE, ESI, PT…); its rates, ceilings and caps live in effective-dated `StatutoryRate` lines
   (with the `StatutorySlab` bands for PT/TDS). Changing a rate next budget = **add a new dated line**,
   never an edit to an existing one — so old runs still recompute on the old line. (Header/detail,
   same shape as `SalaryStructure`; see §3.1.)
3. **Reuse the existing platform.** No new infrastructure: same `SqlDataAccessTransaction`,
   `AuditTrailData`, soft-delete (`Status` bit), `CommonData.LoadTableData<T>` generic
   readers, `Insert_*` upsert procs, `Components/{Input,Button,Card,Dialog,Page}` wrappers,
   `ExportUtils` for Excel/PDF.
4. **A payroll run is immutable once finalized.** Draft → Processed (computed, editable) →
   Finalized (locked, journal posted). Reopening creates an audit entry.
5. **One number, one source.** Net pay, statutory bases, and the accounting journal all
   derive from the same computed `PayrollLine` rows — never recomputed differently in two places.

---

## 2. Solution placement (matches existing conventions)

```
VectusLibrary/Payroll/
  Masters/
    Models/   EmployeeModel, DepartmentModel, DesignationModel, EmployeeLocationModel,
              SalaryComponentModel, SalaryStructureModel, SalaryStructureLineModel,
              StatutoryRuleModel, StatutoryRateModel, StatutorySlabModel
    Data/     EmployeeData, DepartmentData, DesignationData, EmployeeLocationData,
              SalaryComponentData, SalaryStructureData, StatutoryRuleData (rule + dated rate lines + slabs)
    Exports/  EmployeeExport, SalaryStructureExport
  Processing/
    Models/   PayrollRunModel, PayrollLineModel, PayrollLineDetailModel, AttendanceInputModel,
              PayrollAdjustmentModel, ArrearDetailModel, EmployeeAdvanceModel, AdvanceRecoveryModel,
              PayslipModel (vm)
    Data/     PayrollRunData, AttendanceData, PayrollAdjustmentData, ArrearData, EmployeeAdvanceData,
              PayrollCalculationEngine (pure compute, no DB), PayrollArrearEngine,
              PayrollAccountingPoster (builds the FinancialAccounting journal)
    Exports/  PayslipExport (PDF), SalaryRegisterExport, BankTransferExport, SalaryJVExport,
              PFEcrExport (+ Form 12A/5/10/3A/6A), ESIExport (+ RC), PTExport (+ Form III),
              LWFExport, Form24QExport, Form16Export (+ 12BA), BonusFormCDExport, GratuityExport
              — the full statutory set in §9 (all via ExportUtils)

Vectus.Shared/Pages/Payroll/
  Masters/    EmployeePage, DepartmentPage, DesignationPage, EmployeeLocationPage,
              SalaryComponentPage, SalaryStructurePage, StatutoryRulePage
  Processing/ PayrollRunPage, AttendanceEntryPage, PayrollAdjustmentPage, EmployeeAdvancePage
  Reports/    PayslipReport, SalaryRegisterReport, StatutoryReportsPage

DBVectus/Payroll/
  Masters/{Table,Insert,Load}/...
  Processing/{Table,Insert,Load}/...
```

**Name constants:** add a `PayrollNames` static class in `VectusLibrary/Common/DatabaseNames.cs`
(sibling of `AccountNames`, `FleetNames`). Never hardcode table/proc strings.

**Routes:** add `Payroll*` route constants to `VectusLibrary/Common/PageRouteNames.cs`.

**Auth:** add a `Payroll` value to the `UserRoles` enum; every page calls
`AuthenticationService.ValidateUser(..., UserRoles.Payroll)` in `OnAfterRenderAsync(firstRender)`;
re-check `_user.Admin` before mutating actions.

**Platform services:** none needed — payroll reuses existing `ISaveAndViewService` for export viewing.

---

## 3. Data model

### 3.1 Masters

**Department / Designation / EmployeeLocation** — simple `(Id, Name, Code, Status)` masters,
identical shape to `AccountType`. Location optionally carries `StateUTId` (drives PT slab).

**Employee** (`Employee` table)
| Column | Type | Notes |
|---|---|---|
| Id | INT IDENTITY PK | |
| Code | VARCHAR(10) UNIQUE | generated via `GenerateCodes` (e.g. `EMP0001`) |
| Name | VARCHAR(250) | |
| DepartmentId / DesignationId / LocationId | INT FK | |
| DateOfJoining | DATE | |
| DateOfLeaving | DATE NULL | |
| PaymentMode | VARCHAR (Bank/Cash) | |
| BankName / BankAccountNo / IFSC | VARCHAR NULL | |
| PANNo / AadhaarNo | VARCHAR NULL | |
| PFUAN / PFNumber | VARCHAR NULL | |
| ESINumber | VARCHAR NULL | |
| IsPFApplicable / IsESIApplicable / IsPTApplicable / IsLWFApplicable | BIT | per-employee statutory opt-in |
| ContributeOnHigherPFWage | BIT | PF on actual Basic+DA above the ₹15,000 ceiling (voluntary) |
| ESICoveredUpto | DATE NULL | end of the ESI contribution period the employee is locked into — keeps ESI running till Sep/Mar even after gross crosses ₹21,000 |
| Phone / Email / Address | VARCHAR NULL | validated like Ledger |
| Status | BIT DEFAULT 1 | soft delete |

**SalaryComponent** (`SalaryComponent` table) — the heart of the configurability
| Column | Type | Notes |
|---|---|---|
| Id, Name, Code | | |
| ComponentType | VARCHAR | `Earning` / `Deduction` |
| CalculationType | VARCHAR | `Fixed` / `PercentOfBase` / `Slab` / `StatutoryRule` / `AdvanceRecovery` / `Arrear` |
| BaseComponentCode | VARCHAR NULL | for `PercentOfBase` (e.g. HRA = 40% of BASIC) |
| Percentage | DECIMAL(9,4) NULL | |
| StatutoryRuleCode | VARCHAR NULL | links to `StatutoryRule` (PF/ESI/PT/TDS) |
| IsTaxable | BIT | feeds TDS taxable income |
| IsPerquisite | BIT | reported separately on Form 12BA / Form 16 |
| AffectsPFBase / AffectsESIBase | BIT | which components form the statutory base |
| DisplayOrder | INT | payslip ordering |
| Status | BIT | |

**SalaryStructure** + **SalaryStructureLine** (header/detail, like FinancialAccounting+Ledger)
- Header: `Id, EmployeeId, EffectiveFrom, CTC (optional), Status`.
- Line: `Id, SalaryStructureId, SalaryComponentId, Amount (for Fixed), OverridePercentage NULL`.
- An employee can have multiple structures over time; the run picks the one effective for the period.

**StatutoryRule** + **StatutoryRate** + **StatutorySlab** (configurable engine, header/detail — same
shape as `SalaryStructure` + line). The **rule** is the stable identity; the **rate** is the
effective-dated set of numbers; **slabs** are that rate's PT/TDS bands. A rate change is a **new
`StatutoryRate` line**, never an edit to an existing one — so historical and arrear runs recompute on
their own period's line. (The `StatutoryRulePage` adds a dated line instead of overwriting — the one
way it differs from a vanilla master; everything else is the standard CRUD shape.)
- `StatutoryRule` (header — the identity, doesn't change per budget): `Id, Code, Name,
  ContributionAccount NULL, RoundingMode, LedgerId NULL, StateUTId NULL, Status`.
  - `ContributionAccount` is the EPF challan A/c (`A/c 1`/`2`/`10`/`21`/`22`) for the ECR export.
  - `RoundingMode` = `Nearest` (PF) / `Up` (ESI) / `None`.
  - `LedgerId` is the accounts ledger this rule posts to (PF Payable, ESI Payable…) — see §5.
- `StatutoryRate` (detail — the effective-dated numbers): `Id, StatutoryRuleId, EffectiveFrom,
  EmployeeRate, EmployerRate, WageCeiling NULL, MaxAmount NULL, MinAmount NULL,
  MinBasePercentOfGross NULL, StandardDeduction NULL, RebateAmount NULL, RebateIncomeLimit NULL,
  CessPercent NULL, Status`.
  - `MaxAmount` caps a single contribution (EPS = ₹1,250/mo); `MinAmount` floors it (PF admin min ₹500/mo).
  - `MinBasePercentOfGross` enforces the **Labour-Codes 50% wage floor** (PF rule = 50) — see engine.
  - `StandardDeduction` / `RebateAmount` / `RebateIncomeLimit` / `CessPercent` are the **TDS knobs that
    change almost every Budget** — standard deduction (₹75,000), the §87A rebate amount (₹60,000), the
    taxable-income limit it applies up to (₹12,00,000), and the health-&-education cess (4%). They live
    on the TDS rate line, never as engine literals, so a Budget revision is just a new dated line —
    not a code change.
- `StatutorySlab` (PT/TDS bands, owned by the dated rate): `Id, StatutoryRateId, FromAmount, ToAmount,
  FixedAmount, Rate` — so each rate version carries its own bands and a slab revision is a new rate line too.
- **PF is modelled as several rules, not one** — one `StatutoryRule` per EPFO challan account (each with
  its own dated rate line[s]), so the engine stays generic and the ECR is built straight from them:
  | Sub-rule | A/c | Rate | Base / cap |
  |---|---|---|---|
  | `PF-EE` (employee) | A/c 1 | 12% | PF wage |
  | `PF-EPS` (pension) | A/c 10 | 8.33% | min(PF wage, ₹15,000), cap ₹1,250 |
  | `PF-EPF` (employer) | A/c 1 | 12% − EPS | derived, so >₹15k routes excess to EPF |
  | `PF-EDLI` | A/c 21 | 0.50% | min(PF wage, ₹15,000) |
  | `PF-ADMIN` (admin charges) | A/c 2 | 0.50% | PF wage, **min ₹500** |
  | `PF-INSP` (EDLI admin / **inspection charges**) | A/c 22 | 0.00% | PF wage (NIL for un-exempted) |
  - **Exempted establishments** (own PF/EDLI trust) pay *inspection* charges instead of admin:
    A/c 2 → **0.18%**, A/c 22 → **0.005% (min ₹1)**. A company-level `IsPFExempted` /
    `IsEDLIExempted` flag (payroll settings) just selects the effective-dated rate line — no code change.

> **Money precision:** all amount columns are `DECIMAL(18,2)`; all rate/percent columns
> `DECIMAL(9,4)`. Never `FLOAT` — statutory filings must reconcile to the rupee.

### 3.2 Processing

**PayrollRun** (header) — `Id, CompanyId, FinancialYearId, Year, Month, RunDate,
RunStatus (Draft/Processed/Finalized), TotalGross, TotalDeductions, TotalNet,
FinancialAccountingId NULL (set when journal posted), Status`.

**AttendanceInput** — `Id, PayrollRunId, EmployeeId, PayableDays, PresentDays, LOPDays,
OvertimeHours, Status`. Manually entered (per the chosen integration).

**PayrollAdjustment** — `Id, PayrollRunId, EmployeeId, SalaryComponentId, ComponentType,
Amount, Reason, Status`. One-time, run-specific earnings or deductions that don't belong in the
permanent structure — reimbursements, ad-hoc bonus, incentive, fine. The engine folds
these into gross/deductions alongside the structure. (Arrears are a *computed* kind of adjustment —
see below.)

**ArrearDetail** — `Id, EmployeeId, OriginYear, OriginMonth, SalaryComponentId, OldAmount,
NewAmount, Difference, PaidInPayrollRunId NULL, Status`. Generated when a `SalaryStructure` is
saved with a **back-dated `EffectiveFrom`**: for every already-finalized run in the arrear window
the system stores the per-component old→new difference for that origin period. When the arrears are
paid, the rows are stamped with `PaidInPayrollRunId` and surface as an `ARREAR` earning on that run.
Keeping the **origin period** means PF/ESI/PT are applied with *that month's* ceilings and rates,
and TDS / Form 16 can report arrears separately for the employee's §89(1) relief.

**PayrollLine** (one row per employee per run) — `Id, PayrollRunId, EmployeeId,
GrossEarnings, TotalDeductions, NetPay,
PFWage, PFEmployee, PFEmployerEPF, PFEmployerEPS, EDLI, PFAdmin,
ESIWage, ESIEmployee, ESIEmployer, PT, TDS, AdvanceRecovered,
TotalEmployerCost, Status`. The PF/ESI fields are split so the ECR/ESI returns and the
employer-cost view come straight off the line (no recomputation).

**PayrollLineDetail** (one row per component per employee) — `Id, PayrollLineId,
SalaryComponentId, ComponentType, Amount` — drives the payslip line items and the journal.

**EmployeeAdvance** (loan header) — `Id, EmployeeId, AdvanceDate, Amount (principal),
InstallmentAmount, NumberOfInstallments, OutstandingBalance, DisbursementVoucherId NULL
(→ FinancialAccounting, Dr Staff Advance / Cr Bank on issue), Reason, Status`.
An advance is **not** a salary-structure line — it is a separate loan with its own balance.

**AdvanceRecovery** (recovery ledger) — `Id, EmployeeAdvanceId, PayrollRunId, RecoveryDate,
Amount, OutstandingAfter, Status` — one row per installment recovered by a run.
The `ADV` salary component has `CalculationType = AdvanceRecovery`; the engine sums each
employee's due installments (capped at outstanding) into that single deduction line, so
advances reach the payslip and journal without sitting in the structure. Reopening a run
soft-deletes its `AdvanceRecovery` rows and restores the balances.

> All tables follow the standard pattern: `Status` bit, audit on every mutation,
> `Insert_*` upsert proc keyed on `Id == 0`.

---

## 4. The calculation engine (`PayrollCalculationEngine`)

A **pure, DB-free static class** (easy to unit-test, fully portable). Signature roughly:

```
PayrollLine Compute(
    EmployeeModel emp,
    SalaryStructureModel structure,          // effective lines
    AttendanceInputModel attendance,         // payable/LOP days
    IReadOnlyList<PayrollAdjustmentModel> adjustments, // one-time this run
    IReadOnlyList<EmployeeAdvanceModel> advances,      // active loans
    IReadOnlyList<SalaryComponentModel> components,
    IReadOnlyList<StatutoryRuleModel> rules,
    IReadOnlyList<StatutoryRateModel> rates, // the line per rule effective for the run period
    IReadOnlyList<StatutorySlabModel> slabs,
    YtdTotals ytd)                           // year-to-date, for TDS projection
```

Algorithm (order matters):
1. **Pro-rate earnings** by `PresentDays / PayableDays` (LOP) for each `Earning` component,
   resolving `Fixed` and `PercentOfBase` (topologically — Basic first, then dependents).
2. **Add adjustments** — fold `PayrollAdjustment` earnings/deductions for this run.
3. **Gross** = sum of earnings (structure + earning adjustments).
4. **PF wage with 50% floor (Labour Codes).**
   `pfWage = MAX(Σ components where AffectsPFBase, MinBasePercentOfGross% × gross)`.
   Cap at `WageCeiling` (₹15,000) unless `ContributeOnHigherPFWage`.
   - `PF-EE` = 12% × pfWage.
   - `PF-EPS` = 8.33% × MIN(pfWage, 15000), capped at `MaxAmount` (₹1,250).
   - `PF-EPF` (employer) = 12% × pfWage − EPS.
   - `EDLI` = 0.5% × MIN(pfWage,15000); `PF-ADMIN` = 0.5% × pfWage, floored at `MinAmount`.
5. **ESI** applies if `IsESIApplicable` **and** (gross ≤ ESI ceiling **or** run period ≤ `ESICoveredUpto`).
   On crossing the ceiling, set `ESICoveredUpto` to the period end (Sep/Mar) so deductions
   continue to period end. Employee 0.75% / employer 3.25% of gross.
6. **PT** = slab lookup on gross for the employee's state.
7. **TDS** = projected annual taxable (Σ `IsTaxable` earnings × remaining months + YTD) −
   `rate.StandardDeduction` → regime slabs (`StatutorySlab`) → −`rate.RebateAmount` §87A rebate
   (when taxable ≤ `rate.RebateIncomeLimit`) → +`rate.CessPercent` cess → ÷ remaining months.
   Every threshold is read from the effective TDS rate line — **no statutory literal in the engine**.
   (Phase 3; manual override to start.)
8. **Advance recovery** = for each active `EmployeeAdvance` with outstanding > 0, take the due
   installment capped at the outstanding balance; emit `AdvanceRecovery` + reduce the balance.
9. **Rounding** — apply each rule's `RoundingMode` (PF nearest rupee, ESI rounded **up**) before summing.
10. **Net** = Gross − (employee deductions + PF-EE + ESI-EE + PT + TDS + advance + deduction adjustments).
    **Guard:** if Net < 0, cap that run's advance recovery so Net ≥ 0 and carry the shortfall forward.
11. Emit `PayrollLineDetail` rows for every component for transparency.

For each `StatutoryRule` the engine takes the `StatutoryRate` line (and its `StatutorySlab` bands)
with the latest `EffectiveFrom <= run period` — so historical and arrear runs recompute on their own
period's line, and a rate change is just a new dated line, never an edit to an existing one.

### Arrears (`PayrollArrearEngine`)

When a `SalaryStructure` is saved with a back-dated `EffectiveFrom`, arrears are the retrospective
shortfall for the months already paid at the old rate. The arrear engine is a thin wrapper over the
same `Compute`:

1. **Find the window** — finalized runs from `EffectiveFrom` up to the current open run.
2. **Recompute each past period** under the **new** structure (same attendance/LOP that was actually
   used) to get "should-have-been" earnings; subtract what was actually paid (historical
   `PayrollLineDetail`). The per-component positive/negative differences are the `ArrearDetail` rows.
3. **Statutory per origin period** — compute PF / ESI / PT on each period's difference *using that
   period's ceilings and rates*, then sum. (Doing it per-period, not on the lump sum, keeps the
   ₹15,000 PF / ₹21,000 ESI ceilings and any rate change correct.)
4. **Surface in the paying run** — total arrear earnings appear as an `ARREAR` line (broken down by
   origin month on the payslip); the summed arrear PF/ESI add to that run's contributions (EPFO/ESIC
   accept arrear contributions in the month paid); PT is re-checked on the **total gross paid** in the
   paying month (arrears can bump the slab); TDS adds arrears to the annual projection.
5. **Negative arrears** (a retrospective *cut*) become a recovery — handled by the negative-net guard.

**Tax note (employee-side, report-only for us):** arrears are taxable in the year received, but the
employee can claim **§89(1) relief** by filing **Form 10E** (recompute tax for the origin years vs the
receipt year). Payroll's job is to **tag arrears separately** so they show on **Form 16 Part B** and on
a §89(1)/10E working — we don't grant the relief, we provide the figures.

**Re-processing is idempotent:** Process deletes this run's prior `PayrollLine`/`Detail`/
`AdvanceRecovery` rows and recomputes from scratch, so it is safe to re-run a Draft/Processed
run any number of times. A **Finalized** run is locked — attendance, adjustments, and advances
for it cannot change until it is reopened (which reverses the journal and restores balances).

---

## 5. Accounting integration (`PayrollAccountingPoster`)

On **Finalize**, inside one `SqlDataAccessTransaction.Run`, build a journal voucher in the
existing `FinancialAccounting` + `FinancialAccountingLedger` tables:

| Ledger | Dr | Cr |
|---|---|---|
| Salaries & Wages (expense) | Total Gross | |
| Employer statutory cost (expense) | Employer EPF + EPS + EDLI + PF admin + employer ESI | |
| PF Payable / ESI Payable / PT Payable / TDS Payable | | statutory liabilities (incl. EDLI + admin) |
| Staff Advance | | advance recovered (asset reduced) |
| Salary Payable / Bank | | Net pay |

- Ledger mapping is configurable: each `SalaryComponent` (and each `StatutoryRule` header) carries an
  optional `LedgerId` so the poster knows which ledger to hit; a few company-level default
  ledgers (Salary Payable, Bank) live in settings.
- The created `FinancialAccounting.Id` is written back to `PayrollRun.FinancialAccountingId`
  so the run links to its voucher. Reversing a finalize soft-deletes the voucher.

This reuses `FinancialAccountingData.SaveTransaction` — no new accounting logic.

---

## 6. UI pages (Syncfusion grid + existing wrappers)

**Masters** — standard master/CRUD pages (clone `LedgerPage` shape): grid + add/edit dialog,
delete = soft-delete, recover toggle, Excel export. One each for Department, Designation,
Location, Employee, SalaryComponent, SalaryStructure, StatutoryRule. **SalaryStructure** and
**StatutoryRule** are header/detail (grid of rules/structures, each expanding to its effective-dated
lines); "change a rate/structure" **adds a new dated line** rather than overwriting — the only
deviation from the plain CRUD shape.

**Attendance Entry** — pick run period; grid of active employees with editable
PayableDays / PresentDays / LOP / Overtime; bulk save.

**Payroll Run** — the transaction page:
- Create/select run (Company + Year + Month).
- **Process** button → engine computes all lines → editable preview grid (gross/deductions/net per employee, expandable to component detail).
- **Finalize** button → locks run + posts journal (confirmation dialog; Admin only).
- **Reopen** (Admin) → reverses journal, returns to Processed, audited.

**Reports** — Payslip (per employee / bulk PDF), Salary Register (Excel), Bank Transfer (Excel),
Statutory reports (PF ECR text/Excel, ESI, PT). All via `ExportUtils`.

---

## 7. Build phases (deliver + verify each)

- **Phase 1 — Masters & foundation.** Tables/procs + `PayrollNames` + routes + `Payroll` role.
  Pages: Department, Designation, Location, Employee, SalaryComponent, SalaryStructure,
  StatutoryRule. Outcome: you can define employees and pay structures. *No calculation yet.*
- **Phase 2 — Run & payslip.** AttendanceEntry, PayrollAdjustment, EmployeeAdvance (issue +
  recovery schedule), PayrollRun, `PayrollCalculationEngine` (Fixed / %-of-base / PF with EPS/EDLI
  split + 50% floor / ESI with period rule / PT / advance recovery / rounding), payslip PDF (with YTD),
  salary register + bank transfer Excel.
  Outcome: end-to-end monthly payroll, draft→processed→finalized (journal stubbed).
- **Phase 3 — Statutory + accounting.** `PayrollAccountingPoster` (journal into FinancialAccounting),
  TDS engine (projection + 87A — std deduction, rebate, rebate limit and cess all read from the TDS
  rate line, never hardcoded), `PayrollArrearEngine` (retrospective revisions),
  PF ECR (A/c 1/2/10/21/22) / ESI / PT statutory exports, Form 24Q + Form 16 (with arrears) data.
- **Phase 4 — Lifecycle (optional, future).** Gratuity, bonus (Payment of Bonus Act),
  leave management + encashment, full-and-final settlement, investment declarations (Form 12BB),
  employee self-service payslip access.

Each phase ends with: `dotnet build` of VectusLibrary, Shared, and Web all clean; manual run via
`dotnet run --project Vectus/Vectus.Web`.

---

## 7a. Quality & engineering (don't skip)

- **Idempotent processing** — re-Process wipes and recomputes this run's lines; Finalize locks.
- **Snapshot, don't reference** — `PayrollLineDetail` stores the *computed amount*, so a later
  config/rate edit never changes a finalized run's printed payslip.
- **Statutory rates are append-only** — the `StatutoryRule` header is edited in place, but a *rate
  change* inserts a new effective-dated `StatutoryRate` line; used lines are never overwritten, so
  re-processing a draft or computing arrears after a rate change still resolves each period's own rate.
- **Rounding policy** — per-rule `RoundingMode` (PF nearest, ESI up) applied before totals, so the
  module's numbers reconcile to the EPFO/ESIC challans to the rupee.
- **Negative-net guard** — recovery/deductions can never drive net pay below zero; shortfall carries forward.
- **Validation** — bank fields required when `PaymentMode = Bank`; IFSC/PAN/Aadhaar format checks;
  one active `SalaryStructure` per `EffectiveFrom`; `PayableDays > 0`; `PresentDays ≤ PayableDays`.
- **PII / security** — bank a/c, PAN, Aadhaar are sensitive: gate the Employee master behind the
  `Payroll` role + `Admin` for edits, mask Aadhaar in the grid (show last 4), and keep these out of
  general exports. Consider column-level encryption for Aadhaar.
- **Performance** — a run of N employees does N engine passes in memory then one batched transaction;
  load components/rules/slabs once per run, not per employee.

---

## 8. Compliance notes & seed data (India, FY 2026-27)

These are the values to seed the `StatutoryRule` / `StatutoryRate` / `StatutorySlab` tables with. Confirm against the
official source at go-live (per the "use latest docs" rule); they are current as of mid-2026.

**EPF (EPFO)** — model as the sub-rules in §3.2. Un-exempted establishment rates:
| Sub-rule | Rate | Of | Cap / floor | ECR A/c |
|---|---|---|---|---|
| PF employee | 12% | PF wage | — | A/c 1 |
| PF employer → EPS | 8.33% | min(PF wage, ₹15,000) | max ₹1,250 | A/c 10 |
| PF employer → EPF | 12% − EPS | (derived) | — | A/c 1 |
| EDLI | 0.5% | min(PF wage, ₹15,000) | — | A/c 21 |
| PF admin charges | 0.5% | PF wage | min ₹500 | A/c 2 |
| EDLI admin / **inspection charges** | 0.00% (un-exempted) | PF wage | — | A/c 22 |

> **Exempted establishments** pay inspection charges instead: A/c 2 → 0.18%, A/c 22 → 0.005%
> (min ₹1). Driven by the per-company exemption flag — just a different effective-dated rate line.

**ESI (ESIC)** — 0.75% employee + 3.25% employer of gross; wage ceiling **₹21,000**
(₹25,000 for persons with disability). **Contribution-period rule:** coverage at the start of a
period (Apr–Sep / Oct–Mar) continues to that period's end even if gross later crosses the ceiling.

**Professional Tax — West Bengal (monthly)** — `StatutorySlab` rows:
| Monthly gross | PT |
|---|---|
| ≤ ₹10,000 | ₹0 |
| ₹10,001–₹15,000 | ₹110 |
| ₹15,001–₹25,000 | ₹130 |
| ₹25,001–₹40,000 | ₹150 |
| > ₹40,000 | ₹200 |

**Labour Welfare Fund — West Bengal** — fixed half-yearly amounts (not %): employee + employer
contribution, deposited Jun & Dec. Model as a `StatutoryRule` with `FixedAmount` per side; deducted
in the June and December runs only. (Small amounts; exact figures per the WB LWF Act schedule.)

**TDS — new regime, FY 2026-27 (default)** — seed the TDS `StatutoryRate` line's columns:
`StandardDeduction` **₹75,000**, `RebateAmount` **₹60,000** (§87A) up to `RebateIncomeLimit`
**₹12,00,000** taxable (nil tax to ₹12L), `CessPercent` **4%**. Annual slabs as `StatutorySlab` rows:
| Annual taxable | Rate |
|---|---|
| 0 – ₹4,00,000 | Nil |
| ₹4,00,001 – ₹8,00,000 | 5% |
| ₹8,00,001 – ₹12,00,000 | 10% |
| ₹12,00,001 – ₹16,00,000 | 15% |
| ₹16,00,001 – ₹20,00,000 | 20% |
| ₹20,00,001 – ₹24,00,000 | 25% |
| > ₹24,00,000 | 30% |

**Labour Codes 2025 (in force 21 Nov 2025) — the 50% wage rule.** Basic + DA + retaining
allowance must be **≥ 50% of total remuneration**; any shortfall is added back to "wages" for PF
and gratuity. This is why the engine uses `MinBasePercentOfGross` (§4 step 4) rather than a raw
sum of PF-base components. Some state rules are still being notified — keep it configurable.

> Rates above are placeholders to seed defaults. Confirm current figures from the EPFO/ESIC/WB-PT/
> Income-tax sources at implementation time (per the "use latest docs" rule).

---

## 9. Statutory reports & returns (the complete set)

Every report an Indian company must produce is generated from the same finalized `PayrollRun`
data — no re-keying. Organised by authority; ★ = statutory filing, ○ = supporting register/input.
WB-specific items apply because the company is in Durgapur; the report layer is state-driven so
other states are configuration.

**Provident Fund — EPFO**
| Report | Freq | Purpose | Built from |
|---|---|---|---|
| ★ ECR (Electronic Challan-cum-Return) | Monthly | Contribution text file across A/c 1/2/10/21/22, UAN-wise | `PayrollLine` PF fields |
| ★ Form 12A | Monthly | Consolidated contribution statement | run totals |
| ★ Form 5 / Form 10 | Monthly | New joiners / exits during the month | `Employee` DOJ/DOL |
| ★ Form 3A | Annual | Member-wise yearly contribution card | 12 months of lines |
| ★ Form 6A | Annual | Consolidated annual contribution statement | yearly roll-up |
| ○ Form 2 / Form 11 | On joining | Nomination / prior-membership declaration | `Employee` master |

**Employees' State Insurance — ESIC**
| Report | Freq | Purpose | Built from |
|---|---|---|---|
| ★ Monthly contribution file/challan | Monthly | Employee-wise ESI contributions | `PayrollLine` ESI fields |
| ★ Return of Contributions (RC / Form 5) | Half-yearly | Apr–Sep & Oct–Mar return | period roll-up |
| ○ Form 6 / Form 7 | Register | Register of employees / contributions | `Employee`, lines |
| ○ Form 1 / Accident (Form 12,16) | On event | Declaration / accident report | master, ops |

**Professional Tax — West Bengal**
| Report | Freq | Purpose | Built from |
|---|---|---|---|
| ★ PT challan | Monthly | Deposit of PT deducted | `PayrollLine.PT` |
| ★ WB PT Return (Form III) | Annual | Annual reconciliation return | yearly PT |

**TDS / Income Tax**
| Report | Freq | Purpose | Built from |
|---|---|---|---|
| ★ Form 24Q (+ Annexure I/II, Form 27A) | Quarterly | TDS return on salaries | `PayrollLine.TDS` |
| ★ Form 16 (Part A + Part B) | Annual (by 15 Jun) | Employee TDS certificate | yearly + 24Q |
| ★ Form 12BA | Annual | Perquisites statement (with Form 16) | components flagged perquisite |
| ○ Form 12BB | Annual (year start) | Employee investment declaration (TDS input) | declaration capture |

**Labour Welfare Fund — West Bengal**
| Report | Freq | Purpose | Built from |
|---|---|---|---|
| ★ WB LWF return + challan | Half-yearly (Jun & Dec) | Employee + employer LWF | LWF deduction component |

**Bonus — Payment of Bonus Act, 1965**
| Report | Freq | Purpose | Built from |
|---|---|---|---|
| ○ Form A / Form B | Annual | Allocable surplus / set-on-set-off | accounts + payroll |
| ★ Form C | Annual | Bonus disbursed per employee | bonus run |
| ★ Form D | Annual | Annual return to Labour authority | Form C roll-up |

**Gratuity — Payment of Gratuity Act, 1972**
| Report | Freq | Purpose | Built from |
|---|---|---|---|
| ○ Form F | On joining | Nomination | `Employee` master |
| ★ Gratuity computation | On exit | (Last drawn × 15 × years) ÷ 26 | F&F (Phase 4) |

**Labour registers (consolidated under the 2025 Codes)** — Wage register, Muster roll/attendance,
Register of employees, Register of deductions, Loan/advance register. ○ generated from run data.

**Internal MIS (non-statutory but expected)** — Payslip, Salary register, Bank/NEFT transfer file,
Salary journal voucher, CTC/employer-cost report, headcount, month-on-month variance, YTD summary,
statutory reconciliation (book vs challan).

> **Phasing the reports:** monthly filings (ECR, ESI, PT, 24Q) land in **Phase 3** with the
> calculation that feeds them; annual/periodic forms (3A/6A, Form 16/12BA, ESI RC, LWF, Bonus
> Form C/D, gratuity) are added across **Phase 3–4**. All are export builders under
> `Payroll/Processing/Exports`, delegating to `ExportUtils` (Syncfusion XlsIO/Pdf) and plain-text
> writers for the fixed-width ECR/24Q files.

---

## 10. Deployment gate reminder

Shipping any of this to Azure requires the standard gate (see `CLAUDE.md`): bump README
`Latest Version`, `Vectus.Shared.csproj` `<AssemblyVersion>`, Android `versionName`/`versionCode`
together, and switch `SqlDataAccess._databaseConnection` to `Secrets.AzureConnectionString`.
New `.sql` files must be added to the `<Build>` ItemGroup in `DBVectus.sqlproj`.
```
