using VectusLibrary.Common;
using VectusLibrary.Fleet.TripRequest.Models;
using VectusLibrary.Utils.MailUtils;

namespace VectusLibrary.Fleet.TripRequest.Exports;

internal static class TripRequestNotify
{
	internal static async Task Notify(int tripRequestId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		if (type == NotifyType.Created)
			return;

		await NotifyByMail(tripRequestId, type, previousInvoice);
	}

	private static async Task NotifyByMail(int tripRequestId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		var tripRequest = await CommonData.LoadTableDataById<TripRequestOverviewModel>(FleetNames.TripRequestOverview, tripRequestId);

		var emailData = new TransactionMailing.TransactionEmailData
		{
			TransactionType = "Trip Request",
			TransactionNo = tripRequest.TransactionNo,
			Action = type,
			LocationName = tripRequest.VehicleCode,
			Details = new Dictionary<string, string>
			{
				["Transaction Number"] = tripRequest.TransactionNo,
				["Transaction Date"] = tripRequest.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt"),
				["Route"] = $"{tripRequest.FromLocation} to {tripRequest.ToLocation}",
				["Vehicle"] = tripRequest.VehicleCode,
				["SDR"] = tripRequest.SDRName ?? "N/A",
				["Request Status"] = tripRequest.RequestStatus,
				["Estimated Distance"] = tripRequest.EstimatedDistance.ToString("#,##0"),
				["Estimated Cost"] = tripRequest.EstimatedCost.FormatIndianCurrency(),
				[type == NotifyType.Deleted ? "Deleted By" : type == NotifyType.Updated ? "Updated By" : "Modified By"] = tripRequest.LastModifiedByUserName ?? tripRequest.CreatedByName
			},
			Remarks = tripRequest.Remarks
		};

		await TransactionMailing.SendTransactionEmail(emailData);
	}
}
