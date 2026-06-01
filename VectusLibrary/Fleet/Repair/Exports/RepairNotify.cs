using VectusLibrary.Common;
using VectusLibrary.Fleet.Repair.Models;
using VectusLibrary.Utils.ExportUtils;
using VectusLibrary.Utils.MailUtils;

namespace VectusLibrary.Fleet.Repair.Exports;

internal static class RepairNotify
{
	internal static async Task Notify(int repairId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		if (type == NotifyType.Created)
			return;

		await NotifyByMail(repairId, type, previousInvoice);
	}

	private static async Task NotifyByMail(int repairId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		var repair = await CommonData.LoadTableDataById<RepairOverviewModel>(FleetNames.RepairOverview, repairId);

		var emailData = new TransactionMailing.TransactionEmailData
		{
			TransactionType = "Repair",
			TransactionNo = repair.TransactionNo,
			Action = type,
			LocationName = repair.GarageName,
			Details = new Dictionary<string, string>
			{
				["Transaction Number"] = repair.TransactionNo,
				["Transaction Date"] = repair.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt"),
				["Vehicle"] = repair.VehicleCode,
				["Garage"] = repair.GarageName,
				["Current KM"] = repair.CurrentKM?.ToString("#,##0") ?? "N/A",
				["Total Items"] = repair.TotalItems.ToString(),
				["Total Quantity"] = repair.TotalQuantity.ToString("#,##0.00"),
				["Total Amount"] = repair.TotalAmount.FormatIndianCurrency(),
				[type == NotifyType.Deleted ? "Deleted By" : type == NotifyType.Updated ? "Updated By" : "Modified By"] = repair.LastModifiedByUserName ?? repair.CreatedByName
			},
			Remarks = repair.Remarks
		};

		// For update emails, include before and after invoices
		if (type == NotifyType.Updated && previousInvoice.HasValue)
		{
			var (afterStream, afterFileName) = await RepairInvoiceExport.ExportInvoice(repairId, InvoiceExportType.PDF);

			// Rename files to make it clear which is which
			var beforeFileName = $"BEFORE_{previousInvoice.Value.Item2}";
			var afterFileNameWithPrefix = $"AFTER_{afterFileName}";

			emailData.BeforeAttachment = (previousInvoice.Value.Item1, beforeFileName);
			emailData.AfterAttachment = (afterStream, afterFileNameWithPrefix);
		}
		else
		{
			// For delete/recover, just attach the current invoice
			var (pdfStream, pdfFileName) = await RepairInvoiceExport.ExportInvoice(repairId, InvoiceExportType.PDF);
			emailData.Attachments = new Dictionary<MemoryStream, string> { { pdfStream, pdfFileName } };
		}

		await TransactionMailing.SendTransactionEmail(emailData);
	}
}
