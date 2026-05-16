using VectusLibrary.Accounts.FinancialAccounting.Models;
using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.DataAccess;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace VectusLibrary.Common;

public static class GenerateCodes
{
	private static async Task<string> CheckDuplicateCode(string code, int numberLength, CodeType type, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var isDuplicate = true;
		while (isDuplicate)
		{
			switch (type)
			{
				case CodeType.FinancialAccounting:
					var accounting = await CommonData.LoadTableDataByTransactionNo<FinancialAccountingModel>(AccountNames.FinancialAccounting, code, sqlDataAccessTransaction);
					isDuplicate = accounting is not null;
					break;
				case CodeType.Ledger:
					var ledger = await CommonData.LoadTableDataByCode<LedgerModel>(AccountNames.Ledger, code, sqlDataAccessTransaction);
					isDuplicate = ledger is not null;
					break;

					//case CodeType.Driver:
					//	var driver = await CommonData.LoadTableDataByCode<DriverModel>(FleetNames.Driver, code, sqlDataAccessTransaction);
					//	isDuplicate = driver is not null;
					//	break;

					//case CodeType.VehicleType:
					//	var vehicleType = await CommonData.LoadTableDataByCode<VehicleTypeModel>(FleetNames.VehicleType, code, sqlDataAccessTransaction);
					//	isDuplicate = vehicleType is not null;
					//	break;
					//case CodeType.VehicleDocumentType:
					//	var vehicleDocumentType = await CommonData.LoadTableDataByCode<VehicleDocumentTypeModel>(FleetNames.VehicleDocumentType, code, sqlDataAccessTransaction);
					//	isDuplicate = vehicleDocumentType is not null;
					//	break;
			}

			if (!isDuplicate)
				return code;

			var prefix = code[..(code.Length - numberLength)];
			var lastNumberPart = code[(code.Length - numberLength)..];
			if (int.TryParse(lastNumberPart, out int lastNumber))
			{
				int nextNumber = lastNumber + 1;
				code = $"{prefix}{nextNumber.ToString($"D{numberLength}")}";
			}
			else
				code = $"{prefix}{1.ToString($"D{numberLength}")}";
		}
		return code;
	}

	#region Accounts
	public static async Task<string> GenerateFinancialAccountingTransactionNo(FinancialAccountingModel accounting, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, accounting.FinancialYearId, sqlDataAccessTransaction);
		var companyPrefix = (await CommonData.LoadTableDataById<CompanyModel>(AccountNames.Company, accounting.CompanyId, sqlDataAccessTransaction)).Code;
		var accountingPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.FinancialAccountingTransactionPrefix, sqlDataAccessTransaction)).Value;

		var lastAccounting = await CommonData.LoadLastTableDataByFinancialYear<FinancialAccountingModel>(AccountNames.FinancialAccounting, accounting.FinancialYearId, sqlDataAccessTransaction);
		if (lastAccounting is not null)
		{
			var lastTransactionNo = lastAccounting.TransactionNo;
			if (lastTransactionNo.StartsWith($"{companyPrefix}{financialYear.YearNo}{accountingPrefix}"))
			{
				var lastNumberPart = lastTransactionNo[(companyPrefix.Length + financialYear.YearNo.ToString().Length + accountingPrefix.Length)..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{companyPrefix}{financialYear.YearNo}{accountingPrefix}{nextNumber:D5}", 5, CodeType.FinancialAccounting, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{companyPrefix}{financialYear.YearNo}{accountingPrefix}00001", 5, CodeType.FinancialAccounting, sqlDataAccessTransaction);
	}

	public static async Task<string> GenerateLedgerCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var ledgers = await CommonData.LoadTableData<LedgerModel>(AccountNames.Ledger, sqlDataAccessTransaction);
		var ledgerPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.LedgerCodePrefix, sqlDataAccessTransaction)).Value;

		var lastLedger = ledgers.OrderByDescending(l => l.Id).FirstOrDefault();
		if (lastLedger is not null)
		{
			var lastLedgerCode = lastLedger.Code;
			if (lastLedgerCode.StartsWith(ledgerPrefix))
			{
				var lastNumberPart = lastLedgerCode[ledgerPrefix.Length..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{ledgerPrefix}{nextNumber:D5}", 5, CodeType.Ledger, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{ledgerPrefix}00001", 5, CodeType.Ledger, sqlDataAccessTransaction);
	}
	#endregion

	//#region Route
	//public static async Task<string> GenerateDriverCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	//{
	//	var drivers = await CommonData.LoadTableData<DriverModel>(FleetNames.Driver, sqlDataAccessTransaction);
	//	var driverPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.DriverCodePrefix, sqlDataAccessTransaction)).Value;

	//	var lastDriver = drivers.OrderByDescending(vd => vd.Id).FirstOrDefault();
	//	if (lastDriver is not null)
	//	{
	//		var lastDriverCode = lastDriver.Code;
	//		if (lastDriverCode.StartsWith(driverPrefix))
	//		{
	//			var lastNumberPart = lastDriverCode[driverPrefix.Length..];
	//			if (int.TryParse(lastNumberPart, out int lastNumber))
	//			{
	//				int nextNumber = lastNumber + 1;
	//				return await CheckDuplicateCode($"{driverPrefix}{nextNumber:D5}", 5, CodeType.Driver, sqlDataAccessTransaction);
	//			}
	//		}
	//	}

	//	return await CheckDuplicateCode($"{driverPrefix}00001", 5, CodeType.Driver, sqlDataAccessTransaction);
	//}
	//#endregion

	//#region Vehicle
	//public static async Task<string> GenerateVehicleTypeCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	//{
	//	var vehicleTypes = await CommonData.LoadTableData<VehicleTypeModel>(FleetNames.VehicleType, sqlDataAccessTransaction);
	//	var vehicleTypePrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.VehicleTypeCodePrefix, sqlDataAccessTransaction)).Value;

	//	var lastVehicleType = vehicleTypes.OrderByDescending(vt => vt.Id).FirstOrDefault();
	//	if (lastVehicleType is not null)
	//	{
	//		var lastVehicleTypeCode = lastVehicleType.Code;
	//		if (lastVehicleTypeCode.StartsWith(vehicleTypePrefix))
	//		{
	//			var lastNumberPart = lastVehicleTypeCode[vehicleTypePrefix.Length..];
	//			if (int.TryParse(lastNumberPart, out int lastNumber))
	//			{
	//				int nextNumber = lastNumber + 1;
	//				return await CheckDuplicateCode($"{vehicleTypePrefix}{nextNumber:D5}", 5, CodeType.VehicleType, sqlDataAccessTransaction);
	//			}
	//		}
	//	}

	//	return await CheckDuplicateCode($"{vehicleTypePrefix}00001", 5, CodeType.VehicleType, sqlDataAccessTransaction);
	//}

	//public static async Task<string> GenerateVehicleDocumentTypeCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	//{
	//	var vehicleDocumentTypes = await CommonData.LoadTableData<VehicleDocumentTypeModel>(FleetNames.VehicleDocumentType, sqlDataAccessTransaction);
	//	var vehicleDocumentTypePrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.DocumentTypeCodePrefix, sqlDataAccessTransaction)).Value;

	//	var lastVehicleDocumentType = vehicleDocumentTypes.OrderByDescending(vdt => vdt.Id).FirstOrDefault();
	//	if (lastVehicleDocumentType is not null)
	//	{
	//		var lastVehicleDocumentTypeCode = lastVehicleDocumentType.Code;
	//		if (lastVehicleDocumentTypeCode.StartsWith(vehicleDocumentTypePrefix))
	//		{
	//			var lastNumberPart = lastVehicleDocumentTypeCode[vehicleDocumentTypePrefix.Length..];
	//			if (int.TryParse(lastNumberPart, out int lastNumber))
	//			{
	//				int nextNumber = lastNumber + 1;
	//				return await CheckDuplicateCode($"{vehicleDocumentTypePrefix}{nextNumber:D5}", 5, CodeType.VehicleDocumentType, sqlDataAccessTransaction);
	//			}
	//		}
	//	}

	//	return await CheckDuplicateCode($"{vehicleDocumentTypePrefix}00001", 5, CodeType.VehicleDocumentType, sqlDataAccessTransaction);
	//}

	//public static async Task<string> GenerateExpenseTypeCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	//{
	//	var expenseTypes = await CommonData.LoadTableData<ExpenseTypeModel>(FleetNames.ExpenseType, sqlDataAccessTransaction);
	//	var expenseTypePrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.ExpenseTypeCodePrefix, sqlDataAccessTransaction)).Value;

	//	var lastExpenseType = expenseTypes.OrderByDescending(v => v.Id).FirstOrDefault();
	//	if (lastExpenseType is not null)
	//	{
	//		var lastExpenseTypeCode = lastExpenseType.Code;
	//		if (lastExpenseTypeCode.StartsWith(expenseTypePrefix))
	//		{
	//			var lastNumberPart = lastExpenseTypeCode[expenseTypePrefix.Length..];
	//			if (int.TryParse(lastNumberPart, out int lastNumber))
	//			{
	//				int nextNumber = lastNumber + 1;
	//				return await CheckDuplicateCode($"{expenseTypePrefix}{nextNumber:D5}", 5, CodeType.ExpenseType, sqlDataAccessTransaction);
	//			}
	//		}
	//	}

	//	return await CheckDuplicateCode($"{expenseTypePrefix}00001", 5, CodeType.ExpenseType, sqlDataAccessTransaction);
	//}
	//#endregion
}
