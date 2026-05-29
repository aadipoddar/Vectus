namespace VectusLibrary.Common;

public static class PageRouteNames
{
	#region Operations
	public const string Login = "/login";
	public const string LoginWithCode = "/login-with-code";
	public const string LoginWithCodeRedirect = "login-with-code-redirect"; // Do not put leading slash

	public const string Dashboard = "/";

	public const string User = "/operations/user";
	public const string Settings = "/operations/settings";
	public const string AuditTrailReport = "/operations/audit-trail-report";
	#endregion

	#region Accounts
	public const string FinancialAccounting = "/accounts/financial-accounting";

	public const string FinancialAccountingReport = "/accounts/reports/financial-accounting";
	public const string AccountingLedgerReport = "/accounts/reports/accounting-ledger";
	public const string TrialBalanceReport = "/accounts/reports/trial-balance";
	public const string ProfitAndLossReport = "/accounts/reports/profit-and-loss";
	public const string BalanceSheetReport = "/accounts/reports/balance-sheet";

	public const string CompanyMaster = "/accounts/masters/company";
	public const string LedgerMaster = "/accounts/masters/ledger";
	public const string VoucherMaster = "/accounts/masters/voucher";
	public const string GroupMaster = "/accounts/masters/group";
	public const string AccountTypeMaster = "/accounts/masters/account-type";
	public const string FinancialYearMaster = "/accounts/masters/financial-year";
	public const string StateUTMaster = "/accounts/masters/state-ut";
	#endregion

	#region Fleet
	public const string SDRMaster = "/fleet/sdr";
	public const string VehicleMaster = "/fleet/vehicle";
	public const string VehicleTypeMaster = "/fleet/vehicle-type";

	public const string VehicleDocument = "/fleet/vehicle-document";
	public const string VehicleDocumentTypeMaster = "/fleet/vehicle-document-type";
	public const string VehicleDocumentRenewalReport = "/fleet/vehicle-document-renewal-report";

	public const string LocationMaster = "/fleet/location";
	public const string RouteMaster = "/fleet/route";
	public const string DriverMaster = "/fleet/driver";
	public const string VehicleDriverMaster = "/fleet/vehicle-driver";

	public const string TyreCompanyMaster = "/fleet/tyre-company";
	public const string TyreMounting = "/fleet/tyre-mounting";

	public const string TripRequest = "/fleet/trip-request";
	public const string TripRequestMobile = "/fleet/trip-request/mobile";
	public const string TripRequestMobileMap = "/fleet/trip-request/mobile/map";
	public const string TripRequestReport = "/fleet/reports/trip-request";

	public const string Repair = "/fleet/repair";
	public const string RepairReport = "/fleet/reports/repair";
	public const string RepairJobReport = "/fleet/reports/repair-job";

	public const string GarageMaster = "/fleet/garage";
	#endregion
}
