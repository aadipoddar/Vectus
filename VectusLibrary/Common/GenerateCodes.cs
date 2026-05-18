using VectusLibrary.Accounts.FinancialAccounting.Models;
using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.Route.Models;
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

	#region Route
	public static async Task<string> GenerateLocationCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var locations = await CommonData.LoadTableData<LocationModel>(FleetNames.Location, sqlDataAccessTransaction);
		var locationPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.LocationCodePrefix, sqlDataAccessTransaction)).Value;

		var lastLocation = locations.OrderByDescending(l => l.Id).FirstOrDefault();
		if (lastLocation is not null)
		{
			var lastLocationCode = lastLocation.Code;
			if (lastLocationCode.StartsWith(locationPrefix))
			{
				var lastNumberPart = lastLocationCode[locationPrefix.Length..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{locationPrefix}{nextNumber:D5}", 5, CodeType.Location, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{locationPrefix}00001", 5, CodeType.Location, sqlDataAccessTransaction);
	}

	public static async Task<string> GenerateRouteCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var routes = await CommonData.LoadTableData<RouteModel>(FleetNames.Route, sqlDataAccessTransaction);
		var routePrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.RouteCodePrefix, sqlDataAccessTransaction)).Value;

		var lastRoute = routes.OrderByDescending(r => r.Id).FirstOrDefault();
		if (lastRoute is not null)
		{
			var lastRouteCode = lastRoute.Code;
			if (lastRouteCode.StartsWith(routePrefix))
			{
				var lastNumberPart = lastRouteCode[routePrefix.Length..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{routePrefix}{nextNumber:D5}", 5, CodeType.Route, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{routePrefix}00001", 5, CodeType.Route, sqlDataAccessTransaction);
	}

	public static async Task<string> GenerateDriverCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var drivers = await CommonData.LoadTableData<DriverModel>(FleetNames.Driver, sqlDataAccessTransaction);
		var driverPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.DriverCodePrefix, sqlDataAccessTransaction)).Value;

		var lastDriver = drivers.OrderByDescending(vd => vd.Id).FirstOrDefault();
		if (lastDriver is not null)
		{
			var lastDriverCode = lastDriver.Code;
			if (lastDriverCode.StartsWith(driverPrefix))
			{
				var lastNumberPart = lastDriverCode[driverPrefix.Length..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{driverPrefix}{nextNumber:D5}", 5, CodeType.Driver, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{driverPrefix}00001", 5, CodeType.Driver, sqlDataAccessTransaction);
	}
	#endregion

	#region Vehicle Document
	public static async Task<string> GenerateVehicleDocumentTypeCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var vehicleDocumentTypes = await CommonData.LoadTableData<VehicleDocumentTypeModel>(FleetNames.VehicleDocumentType, sqlDataAccessTransaction);
		var vehicleDocumentTypePrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.DocumentTypeCodePrefix, sqlDataAccessTransaction)).Value;

		var lastVehicleDocumentType = vehicleDocumentTypes.OrderByDescending(vdt => vdt.Id).FirstOrDefault();
		if (lastVehicleDocumentType is not null)
		{
			var lastVehicleDocumentTypeCode = lastVehicleDocumentType.Code;
			if (lastVehicleDocumentTypeCode.StartsWith(vehicleDocumentTypePrefix))
			{
				var lastNumberPart = lastVehicleDocumentTypeCode[vehicleDocumentTypePrefix.Length..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{vehicleDocumentTypePrefix}{nextNumber:D5}", 5, CodeType.VehicleDocumentType, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{vehicleDocumentTypePrefix}00001", 5, CodeType.VehicleDocumentType, sqlDataAccessTransaction);
	}
	#endregion

	#region Vehicle
	public static async Task<string> GenerateVehicleTypeCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var vehicleTypes = await CommonData.LoadTableData<VehicleTypeModel>(FleetNames.VehicleType, sqlDataAccessTransaction);
		var vehicleTypePrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.VehicleTypeCodePrefix, sqlDataAccessTransaction)).Value;

		var lastVehicleType = vehicleTypes.OrderByDescending(vt => vt.Id).FirstOrDefault();
		if (lastVehicleType is not null)
		{
			var lastVehicleTypeCode = lastVehicleType.Code;
			if (lastVehicleTypeCode.StartsWith(vehicleTypePrefix))
			{
				var lastNumberPart = lastVehicleTypeCode[vehicleTypePrefix.Length..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{vehicleTypePrefix}{nextNumber:D5}", 5, CodeType.VehicleType, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{vehicleTypePrefix}00001", 5, CodeType.VehicleType, sqlDataAccessTransaction);
	}

	public static async Task<string> GenerateSDRCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var sdrs = await CommonData.LoadTableData<SDRModel>(FleetNames.SDR, sqlDataAccessTransaction);
		var sdrPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.SDRCodePrefix, sqlDataAccessTransaction)).Value;

		var lastSDR = sdrs.OrderByDescending(h => h.Id).FirstOrDefault();
		if (lastSDR is not null)
		{
			var lastSDRCode = lastSDR.Code;
			if (lastSDRCode.StartsWith(sdrPrefix))
			{
				var lastNumberPart = lastSDRCode[sdrPrefix.Length..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{sdrPrefix}{nextNumber:D5}", 5, CodeType.SDR, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{sdrPrefix}00001", 5, CodeType.SDR, sqlDataAccessTransaction);
	}
	#endregion
}
