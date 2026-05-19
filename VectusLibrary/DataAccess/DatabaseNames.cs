namespace VectusLibrary.DataAccess;

public static class OperationNames
{
	#region Common
	public static string LoadTableData => "Load_TableData";
	public static string LoadTableDataById => "Load_TableData_By_Id";
	public static string LoadTableDataByStatus => "Load_TableData_By_Status";
	public static string LoadTableDataByMasterId => "Load_TableData_By_MasterId";
	public static string LoadTableDataByCode => "Load_TableData_By_Code";
	public static string LoadTableDataByTransactionNo => "Load_TableData_By_TransactionNo";
	public static string LoadTableDataByDate => "Load_TableData_By_Date";
	public static string LoadLastTableData => "Load_LastTableData";
	public static string LoadLastTableDataByFinancialYear => "Load_LastTableData_By_FinancialYear";
	public static string LoadLastTableDataByCompanyFinancialYear => "Load_LastTableData_By_Company_FinancialYear";
	public static string LoadCurrentDateTime => "Load_CurrentDateTime";
	#endregion

	#region Settings
	public static string Settings => "Settings";

	public static string UpdateSettings => "Update_Settings";
	public static string LoadSettingsByKey => "Load_Settings_By_Key";
	public static string ResetSettings => "Reset_Settings";
	#endregion

	#region User
	public static string User => "User";
	public static string InsertUser => "Insert_User";
	#endregion

	#region Audit Trail
	public static string AuditTrail => "AuditTrail";
	public static string InsertAuditTrail => "Insert_AuditTrail";
	#endregion
}

public static class AccountNames
{
	#region Financial Accounting
	public static string FinancialAccounting => "FinancialAccounting";
	public static string FinancialAccountingLedger => "FinancialAccountingLedger";

	public static string InsertFinancialAccounting => "Insert_FinancialAccounting";
	public static string InsertFinancialAccountingLedger => "Insert_FinancialAccountingLedger";

	public static string LoadFinancialAccountingByVoucherReference => "Load_FinancialAccounting_By_Voucher_Reference";
	public static string LoadTrialBalanceByCompanyDate => "Load_TrialBalance_By_Company_Date";

	public static string FinancialAccountingOverview => "FinancialAccounting_Overview";
	public static string FinancialAccountingLedgerOverview => "FinancialAccounting_Ledger_Overview";
	#endregion

	#region Masters
	public static string Company => "Company";
	public static string Group => "Group";
	public static string Nature => "Nature";
	public static string AccountType => "AccountType";
	public static string StateUT => "StateUT";
	public static string Ledger => "Ledger";
	public static string Voucher => "Voucher";
	public static string FinancialYear => "FinancialYear";

	public static string InsertCompany => "Insert_Company";
	public static string InsertGroup => "Insert_Group";
	public static string InsertAccountType => "Insert_AccountType";
	public static string InsertStateUT => "Insert_StateUT";
	public static string InsertLedger => "Insert_Ledger";
	public static string InsertVoucher => "Insert_Voucher";
	public static string InsertFinancialYear => "Insert_FinancialYear";

	public static string LoadFinancialYearByDateTime => "Load_FinancialYear_By_DateTime";
	#endregion
}

public static class FleetNames
{
	#region Trip Request
	public static string TripRequest => "TripRequest";

	public static string InsertTripRequest => "Insert_TripRequest";
	#endregion

	#region Route
	public static string Driver => "Driver";
	public static string Location => "Location";
	public static string Route => "Route";

	public static string InsertDriver => "Insert_Driver";
	public static string InsertLocation => "Insert_Location";
	public static string InsertRoute => "Insert_Route";
	#endregion

	#region Vehicle Document
	public static string VehicleDocument => "VehicleDocument";
	public static string VehicleDocumentType => "VehicleDocumentType";

	public static string InsertDocument => "Insert_VehicleDocument";
	public static string InsertDocumentType => "Insert_VehicleDocumentType";
	#endregion

	#region Vehicle
	public static string SDR => "SDR";
	public static string Vehicle => "Vehicle";
	public static string VehicleType => "VehicleType";

	public static string InsertSDR => "Insert_SDR";
	public static string InsertVehicle => "Insert_Vehicle";
	public static string InsertVehicleType => "Insert_VehicleType";
	#endregion
}