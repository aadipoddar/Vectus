using VectusLibrary.Accounts.FinancialAccounting.Exports;
using VectusLibrary.Accounts.FinancialAccounting.Models;
using VectusLibrary.Accounts.Masters.Exports;
using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.Fleet.Garage.Exports;
using VectusLibrary.Fleet.Garage.Models;
using VectusLibrary.Fleet.Repair.Exports;
using VectusLibrary.Fleet.Repair.Models;
using VectusLibrary.Fleet.Route.Exports;
using VectusLibrary.Fleet.Route.Models;
using VectusLibrary.Fleet.TripRequest.Models;
using VectusLibrary.Fleet.Vehicle.Exports;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Fleet.VehicleDocument.Exports;
using VectusLibrary.Fleet.VehicleDocument.Models;
using VectusLibrary.Operations.Models;
using VectusLibrary.Utils.ExportUtils;

namespace VectusLibrary.Common;

public static class DecodeCode
{
	public static async Task<DecodeTransactionNoModel> DecodeTransactionNo(string transactionNo, bool pdf = true, bool excel = true, CodeType? codeType = null)
	{
		if (string.IsNullOrWhiteSpace(transactionNo))
			return null;

		DecodeTransactionNoModel decodeTransactionNoModel = new();

		if (codeType is null)
			decodeTransactionNoModel = await DecodeTransactionType(transactionNo);
		else
			decodeTransactionNoModel.CodeType = codeType.Value;

		switch (decodeTransactionNoModel.CodeType)
		{
			case CodeType.FinancialAccounting:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<FinancialAccountingModel>(AccountNames.FinancialAccounting, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.FinancialAccounting}/{(decodeTransactionNoModel.TransactionModel as FinancialAccountingModel).Id}";
				if (pdf) decodeTransactionNoModel.PDFStream = await FinancialAccountingInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as FinancialAccountingModel).Id, InvoiceExportType.PDF);
				if (excel) decodeTransactionNoModel.ExcelStream = await FinancialAccountingInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as FinancialAccountingModel).Id, InvoiceExportType.Excel);
				break;
			case CodeType.Ledger:
				var ledgers = await CommonData.LoadTableData<LedgerModel>(AccountNames.Ledger);
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<LedgerModel>(AccountNames.Ledger, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.LedgerMaster}";
				if (pdf) decodeTransactionNoModel.PDFStream = await LedgerExport.ExportMaster(ledgers, ReportExportType.PDF);
				if (excel) decodeTransactionNoModel.ExcelStream = await LedgerExport.ExportMaster(ledgers, ReportExportType.Excel);
				break;

			case CodeType.VehicleType:
				var vehicleTypes = await CommonData.LoadTableData<VehicleTypeModel>(FleetNames.VehicleType);
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByCode<VehicleTypeModel>(FleetNames.VehicleType, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.VehicleTypeMaster}/{(decodeTransactionNoModel.TransactionModel as VehicleTypeModel).Id}";
				if (pdf) decodeTransactionNoModel.PDFStream = await VehicleTypeExport.ExportMaster(vehicleTypes, ReportExportType.PDF);
				if (excel) decodeTransactionNoModel.ExcelStream = await VehicleTypeExport.ExportMaster(vehicleTypes, ReportExportType.Excel);
				break;
			case CodeType.SDR:
				var sdrs = await CommonData.LoadTableData<SDRModel>(FleetNames.SDR);
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByCode<SDRModel>(FleetNames.SDR, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.SDRMaster}/{(decodeTransactionNoModel.TransactionModel as SDRModel).Id}";
				if (pdf) decodeTransactionNoModel.PDFStream = await SDRExport.ExportMaster(sdrs, ReportExportType.PDF);
				if (excel) decodeTransactionNoModel.ExcelStream = await SDRExport.ExportMaster(sdrs, ReportExportType.Excel);
				break;

			case CodeType.VehicleDocumentType:
				var vehicleDocumentTypes = await CommonData.LoadTableData<VehicleDocumentTypeModel>(FleetNames.VehicleDocumentType);
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByCode<VehicleDocumentTypeModel>(FleetNames.VehicleDocumentType, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.VehicleDocumentTypeMaster}/{(decodeTransactionNoModel.TransactionModel as VehicleDocumentTypeModel).Id}";
				if (pdf) decodeTransactionNoModel.PDFStream = await VehicleDocumentTypeExport.ExportMaster(vehicleDocumentTypes, ReportExportType.PDF);
				if (excel) decodeTransactionNoModel.ExcelStream = await VehicleDocumentTypeExport.ExportMaster(vehicleDocumentTypes, ReportExportType.Excel);
				break;

			case CodeType.Location:
				var locations = await CommonData.LoadTableData<LocationModel>(FleetNames.Location);
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByCode<LocationModel>(FleetNames.Location, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.LocationMaster}/{(decodeTransactionNoModel.TransactionModel as LocationModel).Id}";
				if (pdf) decodeTransactionNoModel.PDFStream = await LocationExport.ExportMaster(locations, ReportExportType.PDF);
				if (excel) decodeTransactionNoModel.ExcelStream = await LocationExport.ExportMaster(locations, ReportExportType.Excel);
				break;
			case CodeType.Route:
				var routes = await CommonData.LoadTableData<RouteModel>(FleetNames.Route);
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByCode<RouteModel>(FleetNames.Route, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.RouteMaster}/{(decodeTransactionNoModel.TransactionModel as RouteModel).Id}";
				if (pdf) decodeTransactionNoModel.PDFStream = await RouteExport.ExportMaster(routes, ReportExportType.PDF);
				if (excel) decodeTransactionNoModel.ExcelStream = await RouteExport.ExportMaster(routes, ReportExportType.Excel);
				break;
			case CodeType.Driver:
				var drivers = await CommonData.LoadTableData<DriverModel>(FleetNames.Driver);
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByCode<DriverModel>(FleetNames.Driver, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.DriverMaster}/{(decodeTransactionNoModel.TransactionModel as DriverModel).Id}";
				if (pdf) decodeTransactionNoModel.PDFStream = await DriverExport.ExportMaster(drivers, ReportExportType.PDF);
				if (excel) decodeTransactionNoModel.ExcelStream = await DriverExport.ExportMaster(drivers, ReportExportType.Excel);
				break;

			case CodeType.TripRequest:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<TripRequestModel>(FleetNames.TripRequest, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.TripRequest}/{(decodeTransactionNoModel.TransactionModel as TripRequestModel).Id}";
				// if (pdf) decodeTransactionNoModel.PDFStream = await TripRequestExport.ExportMaster(decodeTransactionNoModel.TransactionModel as TripRequestModel, ReportExportType.PDF);
				// if (excel) decodeTransactionNoModel.ExcelStream = await TripRequestExport.ExportMaster(decodeTransactionNoModel.TransactionModel as TripRequestModel, ReportExportType.Excel);
				break;
			case CodeType.Repair:
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByTransactionNo<RepairModel>(FleetNames.Repair, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.Repair}/{(decodeTransactionNoModel.TransactionModel as RepairModel).Id}";
				if (pdf) decodeTransactionNoModel.PDFStream = await RepairInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as RepairModel).Id, InvoiceExportType.PDF);
				if (excel) decodeTransactionNoModel.ExcelStream = await RepairInvoiceExport.ExportInvoice((decodeTransactionNoModel.TransactionModel as RepairModel).Id, InvoiceExportType.Excel);
				break;
			case CodeType.Garage:
				var garages = await CommonData.LoadTableData<GarageModel>(FleetNames.Garage);
				decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByCode<GarageModel>(FleetNames.Garage, transactionNo);
				decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.GarageMaster}/{(decodeTransactionNoModel.TransactionModel as GarageModel).Id}";
				if (pdf) decodeTransactionNoModel.PDFStream = await GarageExport.ExportMaster(garages, ReportExportType.PDF);
				if (excel) decodeTransactionNoModel.ExcelStream = await GarageExport.ExportMaster(garages, ReportExportType.Excel);
				break;

			default:
				break;
		}

		return decodeTransactionNoModel;
	}

	private static async Task<DecodeTransactionNoModel> DecodeTransactionType(string transactionNo)
	{
		DecodeTransactionNoModel decodeTransactionNoModel = new();

		var beforecodeTypePart = "";
		var codeTypePart = "";

		foreach (var character in transactionNo)
		{
			if (char.IsLetter(character))
				beforecodeTypePart += character;

			if (char.IsDigit(character))
				break;
		}

		foreach (var character in transactionNo[(beforecodeTypePart.Length + 2)..])
		{
			if (char.IsLetter(character))
				codeTypePart += character;

			if (char.IsDigit(character))
				break;
		}

		var settings = await CommonData.LoadTableData<SettingsModel>(OperationNames.Settings);

		if (string.IsNullOrWhiteSpace(codeTypePart))
		{
			if (string.IsNullOrWhiteSpace(beforecodeTypePart))
				return decodeTransactionNoModel;

			codeTypePart = beforecodeTypePart;

			var settingsKey = settings.FirstOrDefault(s => s.Value == codeTypePart).Key;
			settingsKey = settingsKey.Replace("CodePrefix", "");
			decodeTransactionNoModel.CodeType = Enum.Parse<CodeType>(settingsKey);
		}

		else
		{
			var settingsKey = settings.FirstOrDefault(s => s.Value == codeTypePart).Key;
			settingsKey = settingsKey.Replace("TransactionPrefix", "");
			decodeTransactionNoModel.CodeType = Enum.Parse<CodeType>(settingsKey);
		}

		return decodeTransactionNoModel;
	}
}
