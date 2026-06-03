namespace VectusLibrary.Operations.Models;

public class SettingsModel
{
	public string Key { get; set; }
	public string Value { get; set; }
	public string Description { get; set; }
}

public static class SettingsKeys
{
	// Primary Configuration
	public static string PrimaryCompanyLinkingId => "PrimaryCompanyLinkingId";

	// Login Settings
	public static string EnableLoginWithCode => "EnableLoginWithCode";
	public static string EnableUsersToResetPassword => "EnableUsersToResetPassword";
	public static string MaxLoginAttempts => "MaxLoginAttempts";
	public static string CodeResendLimit => "CodeResendLimit";
	public static string CodeExpiryMinutes => "CodeExpiryMinutes";

	// Master Code Prefixes
	public static string LedgerCodePrefix => "LedgerCodePrefix";
	public static string EmployeeCodePrefix => "EmployeeCodePrefix";
	public static string SDRCodePrefix => "SDRCodePrefix";
	public static string VehicleTypeCodePrefix => "VehicleTypeCodePrefix";
	public static string DocumentTypeCodePrefix => "DocumentTypeCodePrefix";
	public static string DriverCodePrefix => "DriverCodePrefix";
	public static string LocationCodePrefix => "LocationCodePrefix";
	public static string RouteCodePrefix => "RouteCodePrefix";
	public static string GarageCodePrefix => "GarageCodePrefix";
	public static string TyreCompanyCodePrefix => "TyreCompanyCodePrefix";

	// Transaction Prefixes
	public static string FinancialAccountingTransactionPrefix => "FinancialAccountingTransactionPrefix";
	public static string TripRequestTransactionPrefix => "TripRequestTransactionPrefix";
	public static string RepairTransactionPrefix => "RepairTransactionPrefix";

	// Ledger Linking
	public static string CashLedgerId => "CashLedgerId";
	public static string GSTLedgerId => "GSTLedgerId";

	// Bank Reconciliation
	public static string BankAccountTypeId => "BankAccountTypeId";

	// Default Values
	public static string DefaultSelectedVoucherId => "DefaultSelectedVoucherId";

	// Fuel & Mileage
	public static string TruckMileageKmPerLitre => "TruckMileageKmPerLitre";
	public static string DieselPricePerLitre => "DieselPricePerLitre";

	// Report Settings
	public static string AutoRefreshReportTimer => "AutoRefreshReportTimer";
	public static string ReportWarningDays => "ReportWarningDays";
	public static string AnalysisCacheHours => "AnalysisCacheHours";
}