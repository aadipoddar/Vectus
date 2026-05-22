using VectusLibrary.Accounts.FinancialAccounting.Models;
using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.Garage.Models;
using VectusLibrary.Fleet.Repair.Models;
using VectusLibrary.Fleet.Route.Models;
using VectusLibrary.Fleet.TripRequest.Models;
using VectusLibrary.Fleet.Tyre.Models;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Fleet.VehicleDocument.Models;
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

				case CodeType.VehicleDocumentType:
					var vehicleDocumentType = await CommonData.LoadTableDataByCode<VehicleDocumentTypeModel>(FleetNames.VehicleDocumentType, code, sqlDataAccessTransaction);
					isDuplicate = vehicleDocumentType is not null;
					break;
				case CodeType.VehicleType:
					var vehicleType = await CommonData.LoadTableDataByCode<VehicleTypeModel>(FleetNames.VehicleType, code, sqlDataAccessTransaction);
					isDuplicate = vehicleType is not null;
					break;
				case CodeType.SDR:
					var sdr = await CommonData.LoadTableDataByCode<SDRModel>(FleetNames.SDR, code, sqlDataAccessTransaction);
					isDuplicate = sdr is not null;
					break;

				case CodeType.Driver:
					var driver = await CommonData.LoadTableDataByCode<DriverModel>(FleetNames.Driver, code, sqlDataAccessTransaction);
					isDuplicate = driver is not null;
					break;
				case CodeType.Location:
					var location = await CommonData.LoadTableDataByCode<LocationModel>(FleetNames.Location, code, sqlDataAccessTransaction);
					isDuplicate = location is not null;
					break;
				case CodeType.Route:
					var route = await CommonData.LoadTableDataByCode<RouteModel>(FleetNames.Route, code, sqlDataAccessTransaction);
					isDuplicate = route is not null;
					break;

				case CodeType.TripRequest:
					var tripRequest = await CommonData.LoadTableDataByTransactionNo<TripRequestModel>(FleetNames.TripRequest, code, sqlDataAccessTransaction);
					isDuplicate = tripRequest is not null;
					break;

				case CodeType.Repair:
					var repair = await CommonData.LoadTableDataByTransactionNo<RepairModel>(FleetNames.Repair, code, sqlDataAccessTransaction);
					isDuplicate = repair is not null;
					break;

				case CodeType.Garage:
					var garage = await CommonData.LoadTableDataByCode<GarageModel>(FleetNames.Garage, code, sqlDataAccessTransaction);
					isDuplicate = garage is not null;
					break;

				case CodeType.TyreCompany:
					var tyreCompany = await CommonData.LoadTableDataByCode<TyreCompanyModel>(FleetNames.TyreCompany, code, sqlDataAccessTransaction);
					isDuplicate = tyreCompany is not null;
					break;
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
		var itemPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.FinancialAccountingTransactionPrefix, sqlDataAccessTransaction)).Value;

		var lastItem = await CommonData.LoadLastTableDataByFinancialYear<FinancialAccountingModel>(AccountNames.FinancialAccounting, accounting.FinancialYearId, sqlDataAccessTransaction);
		if (lastItem is not null)
		{
			var lastItemCode = lastItem.TransactionNo;
			if (lastItemCode.StartsWith($"{companyPrefix}{financialYear.YearNo}{itemPrefix}"))
			{
				var lastNumberPart = lastItemCode[(companyPrefix.Length + financialYear.YearNo.ToString().Length + itemPrefix.Length)..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{companyPrefix}{financialYear.YearNo}{itemPrefix}{nextNumber:D5}", 5, CodeType.FinancialAccounting, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{companyPrefix}{financialYear.YearNo}{itemPrefix}00001", 5, CodeType.FinancialAccounting, sqlDataAccessTransaction);
	}

	public static async Task<string> GenerateLedgerCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var items = await CommonData.LoadTableData<LedgerModel>(AccountNames.Ledger, sqlDataAccessTransaction);
		var itemPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.LedgerCodePrefix, sqlDataAccessTransaction)).Value;

		var lastItem = items.OrderByDescending(l => l.Id).FirstOrDefault();
		if (lastItem is not null)
		{
			var lastItemCode = lastItem.Code;
			if (lastItemCode.StartsWith(itemPrefix))
			{
				var lastNumberPart = lastItemCode[itemPrefix.Length..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{itemPrefix}{nextNumber:D5}", 5, CodeType.Ledger, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{itemPrefix}00001", 5, CodeType.Ledger, sqlDataAccessTransaction);
	}
	#endregion

	#region Vehicle
	public static async Task<string> GenerateVehicleTypeCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var items = await CommonData.LoadTableData<VehicleTypeModel>(FleetNames.VehicleType, sqlDataAccessTransaction);
		var itemPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.VehicleTypeCodePrefix, sqlDataAccessTransaction)).Value;

		var lastItem = items.OrderByDescending(vt => vt.Id).FirstOrDefault();
		if (lastItem is not null)
		{
			var lastItemCode = lastItem.Code;
			if (lastItemCode.StartsWith(itemPrefix))
			{
				var lastNumberPart = lastItemCode[itemPrefix.Length..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{itemPrefix}{nextNumber:D5}", 5, CodeType.VehicleType, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{itemPrefix}00001", 5, CodeType.VehicleType, sqlDataAccessTransaction);
	}

	public static async Task<string> GenerateSDRCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var items = await CommonData.LoadTableData<SDRModel>(FleetNames.SDR, sqlDataAccessTransaction);
		var itemPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.SDRCodePrefix, sqlDataAccessTransaction)).Value;

		var lastItem = items.OrderByDescending(h => h.Id).FirstOrDefault();
		if (lastItem is not null)
		{
			var lastItemCode = lastItem.Code;
			if (lastItemCode.StartsWith(itemPrefix))
			{
				var lastNumberPart = lastItemCode[itemPrefix.Length..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{itemPrefix}{nextNumber:D5}", 5, CodeType.SDR, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{itemPrefix}00001", 5, CodeType.SDR, sqlDataAccessTransaction);
	}
	#endregion

	#region Vehicle Document
	public static async Task<string> GenerateVehicleDocumentTypeCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var items = await CommonData.LoadTableData<VehicleDocumentTypeModel>(FleetNames.VehicleDocumentType, sqlDataAccessTransaction);
		var itemPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.DocumentTypeCodePrefix, sqlDataAccessTransaction)).Value;

		var lastItem = items.OrderByDescending(vdt => vdt.Id).FirstOrDefault();
		if (lastItem is not null)
		{
			var lastItemCode = lastItem.Code;
			if (lastItemCode.StartsWith(itemPrefix))
			{
				var lastNumberPart = lastItemCode[itemPrefix.Length..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{itemPrefix}{nextNumber:D5}", 5, CodeType.VehicleDocumentType, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{itemPrefix}00001", 5, CodeType.VehicleDocumentType, sqlDataAccessTransaction);
	}
	#endregion

	#region Route
	public static async Task<string> GenerateLocationCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var items = await CommonData.LoadTableData<LocationModel>(FleetNames.Location, sqlDataAccessTransaction);
		var itemPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.LocationCodePrefix, sqlDataAccessTransaction)).Value;

		var lastItem = items.OrderByDescending(l => l.Id).FirstOrDefault();
		if (lastItem is not null)
		{
			var lastItemCode = lastItem.Code;
			if (lastItemCode.StartsWith(itemPrefix))
			{
				var lastNumberPart = lastItemCode[itemPrefix.Length..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{itemPrefix}{nextNumber:D5}", 5, CodeType.Location, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{itemPrefix}00001", 5, CodeType.Location, sqlDataAccessTransaction);
	}

	public static async Task<string> GenerateRouteCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var items = await CommonData.LoadTableData<RouteModel>(FleetNames.Route, sqlDataAccessTransaction);
		var itemPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.RouteCodePrefix, sqlDataAccessTransaction)).Value;

		var lastItem = items.OrderByDescending(r => r.Id).FirstOrDefault();
		if (lastItem is not null)
		{
			var lastItemCode = lastItem.Code;
			if (lastItemCode.StartsWith(itemPrefix))
			{
				var lastNumberPart = lastItemCode[itemPrefix.Length..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{itemPrefix}{nextNumber:D5}", 5, CodeType.Route, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{itemPrefix}00001", 5, CodeType.Route, sqlDataAccessTransaction);
	}

	public static async Task<string> GenerateDriverCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var items = await CommonData.LoadTableData<DriverModel>(FleetNames.Driver, sqlDataAccessTransaction);
		var itemPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.DriverCodePrefix, sqlDataAccessTransaction)).Value;

		var lastItem = items.OrderByDescending(vd => vd.Id).FirstOrDefault();
		if (lastItem is not null)
		{
			var lastItemCode = lastItem.Code;
			if (lastItemCode.StartsWith(itemPrefix))
			{
				var lastNumberPart = lastItemCode[itemPrefix.Length..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{itemPrefix}{nextNumber:D5}", 5, CodeType.Driver, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{itemPrefix}00001", 5, CodeType.Driver, sqlDataAccessTransaction);
	}
	#endregion

	#region Trip Request
	public static async Task<string> GenerateTripRequestTransactionNo(TripRequestModel tripRequest, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, tripRequest.FinancialYearId, sqlDataAccessTransaction);
		var companyPrefix = (await CommonData.LoadTableDataById<CompanyModel>(AccountNames.Company, tripRequest.CompanyId, sqlDataAccessTransaction)).Code;
		var itemPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.TripRequestTransactionPrefix, sqlDataAccessTransaction)).Value;

		var lastItem = await CommonData.LoadLastTableDataByFinancialYear<TripRequestModel>(FleetNames.TripRequest, tripRequest.FinancialYearId, sqlDataAccessTransaction);
		if (lastItem is not null)
		{
			var lastItemCode = lastItem.TransactionNo;
			if (lastItemCode.StartsWith($"{companyPrefix}{financialYear.YearNo}{itemPrefix}"))
			{
				var lastNumberPart = lastItemCode[(companyPrefix.Length + financialYear.YearNo.ToString().Length + itemPrefix.Length)..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{companyPrefix}{financialYear.YearNo}{itemPrefix}{nextNumber:D5}", 5, CodeType.FinancialAccounting, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{companyPrefix}{financialYear.YearNo}{itemPrefix}00001", 5, CodeType.FinancialAccounting, sqlDataAccessTransaction);
	}
	#endregion

	#region Repair
	public static async Task<string> GenerateRepairTransactionNo(RepairModel repair, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, repair.FinancialYearId, sqlDataAccessTransaction);
		var companyPrefix = (await CommonData.LoadTableDataById<CompanyModel>(AccountNames.Company, repair.CompanyId, sqlDataAccessTransaction)).Code;
		var itemPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.RepairTransactionPrefix, sqlDataAccessTransaction)).Value;

		var lastItem = await CommonData.LoadLastTableDataByFinancialYear<RepairModel>(FleetNames.Repair, repair.FinancialYearId, sqlDataAccessTransaction);
		if (lastItem is not null)
		{
			var lastItemCode = lastItem.TransactionNo;
			if (lastItemCode.StartsWith($"{companyPrefix}{financialYear.YearNo}{itemPrefix}"))
			{
				var lastNumberPart = lastItemCode[(companyPrefix.Length + financialYear.YearNo.ToString().Length + itemPrefix.Length)..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{companyPrefix}{financialYear.YearNo}{itemPrefix}{nextNumber:D5}", 5, CodeType.Repair, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{companyPrefix}{financialYear.YearNo}{itemPrefix}00001", 5, CodeType.Repair, sqlDataAccessTransaction);
	}
	#endregion

	#region Garage
	public static async Task<string> GenerateGarageCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var items = await CommonData.LoadTableData<GarageModel>(FleetNames.Garage, sqlDataAccessTransaction);
		var itemPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.GarageCodePrefix, sqlDataAccessTransaction)).Value;

		var lastItem = items.OrderByDescending(l => l.Id).FirstOrDefault();
		if (lastItem is not null)
		{
			var lastItemCode = lastItem.Code;
			if (lastItemCode.StartsWith(itemPrefix))
			{
				var lastNumberPart = lastItemCode[itemPrefix.Length..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{itemPrefix}{nextNumber:D5}", 5, CodeType.Garage, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{itemPrefix}00001", 5, CodeType.Garage, sqlDataAccessTransaction);
	}
	#endregion

	#region Tyre
	public static async Task<string> GenerateTyreCompanyCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var items = await CommonData.LoadTableData<TyreCompanyModel>(FleetNames.TyreCompany, sqlDataAccessTransaction);
		var itemPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.TyreCompanyCodePrefix, sqlDataAccessTransaction)).Value;

		var lastItem = items.OrderByDescending(tc => tc.Id).FirstOrDefault();
		if (lastItem is not null)
		{
			var lastItemCode = lastItem.Code;
			if (lastItemCode.StartsWith(itemPrefix))
			{
				var lastNumberPart = lastItemCode[itemPrefix.Length..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{itemPrefix}{nextNumber:D5}", 5, CodeType.TyreCompany, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{itemPrefix}00001", 5, CodeType.TyreCompany, sqlDataAccessTransaction);
	}
	#endregion
}
