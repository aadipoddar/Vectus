using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.Common;
using VectusLibrary.Fleet.Repair.Models;
using VectusLibrary.Utils.ExportUtils;

namespace VectusLibrary.Fleet.Repair.Exports;

public static class RepairInvoiceExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId, InvoiceExportType exportType)
	{
		var transaction = await CommonData.LoadTableDataById<RepairOverviewModel>(FleetNames.RepairOverview, transactionId) ??
			throw new InvalidOperationException("Transaction not found.");

		var jobs = await CommonData.LoadTableDataByMasterId<RepairJobOverviewModel>(FleetNames.RepairJobOverview, transaction.Id);
		if (jobs is null || jobs.Count == 0)
			throw new InvalidOperationException("No repair jobs found for the transaction.");

		var company = await CommonData.LoadTableDataById<CompanyModel>(AccountNames.Company, transaction.CompanyId) ??
			throw new InvalidOperationException("Company information is missing.");

		LedgerModel ledger = new()
		{
			Name = $"Vehicle: {transaction.VehicleCode}"
		};

		var invoiceData = new InvoiceData
		{
			Company = company,
			BillTo = ledger,
			InvoiceType = "REPAIR",
			TransactionNo = transaction.TransactionNo,
			TransactionDateTime = transaction.TransactionDateTime,
			TotalAmount = transaction.TotalAmount,
			Remarks = transaction.Remarks ?? string.Empty,
			Status = transaction.Status
		};

		var columnSettings = new List<InvoiceColumnSetting>
		{
			new("#", "#", exportType, CellAlignment.Center, 25, 5),
			new(nameof(RepairJobOverviewModel.Job), "Job", exportType, CellAlignment.Left, 0, 35),
			new(nameof(RepairJobOverviewModel.Quantity), "Qty", exportType, CellAlignment.Right, 70, 15, "#,##0.00"),
			new(nameof(RepairJobOverviewModel.Rate), "Rate", exportType, CellAlignment.Right, 70, 15, "#,##0.00"),
			new(nameof(RepairJobOverviewModel.Total), "Total", exportType, CellAlignment.Right, 70, 15, "#,##0.00"),
			new(nameof(RepairJobOverviewModel.JobRemarks), "Remarks", exportType, CellAlignment.Left, 100, 25)
		};

		var summaryFields = new Dictionary<string, string>
		{
			["Garage"] = transaction.GarageName,
			["Current KM"] = transaction.CurrentKM?.ToString("#,##0") ?? "-",
			["Total Items"] = transaction.TotalItems.ToString("#,##0"),
			["Total Quantity"] = transaction.TotalQuantity.ToString("#,##0.00"),
			["Total Amount"] = transaction.TotalAmount.FormatIndianCurrency()
		};

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		string fileName = $"REPAIR_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == InvoiceExportType.PDF)
		{
			var stream = await PDFInvoiceExportUtil.ExportInvoiceToPdf(
				invoiceData,
				jobs,
				columnSettings,
				null,
				summaryFields
			);

			fileName += ".pdf";
			return (stream, fileName);
		}
		else
		{
			var stream = await ExcelInvoiceExportUtil.ExportInvoiceToExcel(
				invoiceData,
				jobs,
				columnSettings,
				null,
				summaryFields
			);

			fileName += ".xlsx";
			return (stream, fileName);
		}
	}
}
