namespace VectusLibrary.Common;

public static class CommonNames
{
	public static string LoadTableData => "Load_TableData";
	public static string LoadTableDataById => "Load_TableData_By_Id";
	public static string LoadTableDataByStatus => "Load_TableData_By_Status";
	public static string LoadTableDataByMasterId => "Load_TableData_By_MasterId";
	public static string LoadTableDataByFinancialAccountingId => "Load_TableData_By_FinancialAccountingId";
	public static string LoadTableDataByCode => "Load_TableData_By_Code";
	public static string LoadTableDataByTransactionNo => "Load_TableData_By_TransactionNo";
	public static string LoadTableDataByDate => "Load_TableData_By_Date";
	public static string LoadLastTableData => "Load_LastTableData";
	public static string LoadLastTableDataByFinancialYear => "Load_LastTableData_By_FinancialYear";
	public static string LoadLastTableDataByCompanyFinancialYear => "Load_LastTableData_By_Company_FinancialYear";
	public static string LoadCurrentDateTime => "Load_CurrentDateTime";
}

public static class OperationNames
{

	#region Settings
	public static string Settings => "Settings";

	public static string UpdateSettings => "Update_Settings";
	public static string LoadSettingsByKey => "Load_Settings_By_Key";
	public static string ResetSettings => "Reset_Settings";
	#endregion

	#region User
	public static string User => "User";
	public static string InsertUser => "Insert_User";
	public static string LoadUserByPhoneEmail => "Load_User_By_Phone_Email";
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

public static class PayrollNames
{
	#region Masters
	public static string Department => "Department";
	public static string Designation => "Designation";
	public static string EmployeeLocation => "EmployeeLocation";
	public static string Employee => "Employee";
	public static string SalaryComponent => "SalaryComponent";
	public static string SalaryStructure => "SalaryStructure";
	public static string SalaryStructureLine => "SalaryStructureLine";
	public static string StatutoryRule => "StatutoryRule";
	public static string StatutoryRate => "StatutoryRate";
	public static string StatutorySlab => "StatutorySlab";

	public static string InsertDepartment => "Insert_Department";
	public static string InsertDesignation => "Insert_Designation";
	public static string InsertEmployeeLocation => "Insert_EmployeeLocation";
	public static string InsertEmployee => "Insert_Employee";
	public static string InsertSalaryComponent => "Insert_SalaryComponent";
	public static string InsertSalaryStructure => "Insert_SalaryStructure";
	public static string InsertSalaryStructureLine => "Insert_SalaryStructureLine";
	public static string InsertStatutoryRule => "Insert_StatutoryRule";
	public static string InsertStatutoryRate => "Insert_StatutoryRate";
	public static string InsertStatutorySlab => "Insert_StatutorySlab";
	#endregion
}

public static class FleetNames
{
	#region Garage
	public static string Garage => "Garage";

	public static string InsertGarage => "Insert_Garage";
	#endregion

	#region Trip Request
	public static string TripRequest => "TripRequest";

	public static string InsertTripRequest => "Insert_TripRequest";

	public static string TripRequestOverview => "TripRequest_Overview";

	public static string LoadTripRequestBySDRRequestStatus => "Load_TripRequest_By_SDR_RequestStatus";
	#endregion

	#region Repair
	public static string Repair => "Repair";
	public static string RepairJob => "RepairJob";

	public static string InsertRepair => "Insert_Repair";
	public static string InsertRepairJob => "Insert_RepairJob";

	public static string RepairOverview => "Repair_Overview";
	public static string RepairJobOverview => "RepairJob_Overview";

	public static string LoadGarageVehiclesByDate => "Load_GarageVehicles_By_Date";
	public static string CheckRepairOverlap => "Check_Repair_Overlap";
	#endregion

	#region Route
	public static string VehicleDriver => "VehicleDriver";
	public static string Driver => "Driver";
	public static string Location => "Location";
	public static string Route => "Route";

	public static string InsertVehicleDriver => "Insert_VehicleDriver";
	public static string InsertDriver => "Insert_Driver";
	public static string InsertLocation => "Insert_Location";
	public static string InsertRoute => "Insert_Route";

	public static string DeleteVehicleDriver => "Delete_VehicleDriver";
	#endregion

	#region Vehicle Document
	public static string VehicleDocument => "VehicleDocument";
	public static string VehicleDocumentType => "VehicleDocumentType";

	public static string VehicleDocumentRenewalOverview => "VehicleDocument_Renewal_Overview";

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

	#region Tyre
	public static string TyreCompany => "TyreCompany";
	public static string TyreMounting => "TyreMounting";

	public static string InsertTyreCompany => "Insert_TyreCompany";
	public static string InsertTyreMounting => "Insert_TyreMounting";

	public static string DeleteTyreMounting => "Delete_TyreMounting";
	#endregion
}