using VectusLibrary.Accounts.FinancialAccounting.Exports;
using VectusLibrary.Accounts.FinancialAccounting.Models;
using VectusLibrary.Accounts.Masters.Exports;
using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.DataAccess;
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

			//case CodeType.Driver:
			//	var drivers = await CommonData.LoadTableData<DriverModel>(FleetNames.Driver);
			//	decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByCode<DriverModel>(FleetNames.Driver, transactionNo);
			//	decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.DriverMaster}/{(decodeTransactionNoModel.TransactionModel as DriverModel).Id}";
			//	if (pdf) decodeTransactionNoModel.PDFStream = await DriverExport.ExportMaster(drivers, ReportExportType.PDF);
			//	if (excel) decodeTransactionNoModel.ExcelStream = await DriverExport.ExportMaster(drivers, ReportExportType.Excel);
			//	break;

			//case CodeType.VehicleType:
			//	var vehicleTypes = await CommonData.LoadTableData<VehicleTypeModel>(FleetNames.VehicleType);
			//	decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByCode<VehicleTypeModel>(FleetNames.VehicleType, transactionNo);
			//	decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.VehicleTypeMaster}/{(decodeTransactionNoModel.TransactionModel as VehicleTypeModel).Id}";
			//	if (pdf) decodeTransactionNoModel.PDFStream = await VehicleTypeExport.ExportMaster(vehicleTypes, ReportExportType.PDF);
			//	if (excel) decodeTransactionNoModel.ExcelStream = await VehicleTypeExport.ExportMaster(vehicleTypes, ReportExportType.Excel);
			//	break;
			//case CodeType.VehicleDocumentType:
			//	var vehicleDocumentTypes = await CommonData.LoadTableData<VehicleDocumentTypeModel>(FleetNames.VehicleDocumentType);
			//	decodeTransactionNoModel.TransactionModel = await CommonData.LoadTableDataByCode<VehicleDocumentTypeModel>(FleetNames.VehicleDocumentType, transactionNo);
			//	decodeTransactionNoModel.PageRouteName = $"{PageRouteNames.VehicleDocumentTypeMaster}/{(decodeTransactionNoModel.TransactionModel as VehicleDocumentTypeModel).Id}";
			//	if (pdf) decodeTransactionNoModel.PDFStream = await VehicleDocumentTypeExport.ExportMaster(vehicleDocumentTypes, ReportExportType.PDF);
			//	if (excel) decodeTransactionNoModel.ExcelStream = await VehicleDocumentTypeExport.ExportMaster(vehicleDocumentTypes, ReportExportType.Excel);
			//	break;

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
