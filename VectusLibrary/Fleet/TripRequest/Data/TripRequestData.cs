using VectusLibrary.Accounts.Masters.Data;
using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.TripRequest.Exports;
using VectusLibrary.Fleet.TripRequest.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;
using VectusLibrary.Utils.MailUtils;

namespace VectusLibrary.Fleet.TripRequest.Data;

public static class TripRequestData
{
	public static async Task<int> InsertTripRequest(TripRequestModel tripRequest, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(FleetNames.InsertTripRequest, tripRequest, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Trip Request.");

	public static async Task<List<TripRequestOverviewModel>> LoadBySDRRequestStatus(int? SDRId = null, string RequestStatus = null) =>
		await SqlDataAccess.LoadData<TripRequestOverviewModel, dynamic>(
			FleetNames.LoadTripRequestBySDRRequestStatus, new { SDRId, RequestStatus });

	public static async Task DeleteTransaction(TripRequestModel tripRequest)
	{
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			tripRequest.Status = false;
			await InsertTripRequest(tripRequest, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = FleetNames.TripRequest,
				RecordNo = tripRequest.TransactionNo,
				CreatedBy = tripRequest.LastModifiedBy.Value,
				CreatedFromPlatform = tripRequest.LastModifiedFromPlatform
			}, transaction);
		});

		await TripRequestNotify.Notify(tripRequest.Id, NotifyType.Deleted);
	}

	public static async Task RecoverTransaction(TripRequestModel tripRequest)
	{
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			tripRequest.Status = true;
			await InsertTripRequest(tripRequest, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = FleetNames.TripRequest,
				RecordNo = tripRequest.TransactionNo,
				CreatedBy = tripRequest.LastModifiedBy.Value,
				CreatedFromPlatform = tripRequest.LastModifiedFromPlatform
			}, transaction);
		});

		await TripRequestNotify.Notify(tripRequest.Id, NotifyType.Recovered);
	}

	private static async Task ValidateTransaction(TripRequestModel item, bool update)
	{
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (item.CompanyId <= 0)
			throw new Exception("Company is required. Please select a valid company.");

		if (item.RouteId <= 0)
			throw new Exception("Route is required. Please select a valid route.");

		if (item.VehicleId <= 0)
			throw new Exception("Vehicle is required. Please select a valid vehicle.");

		var financialYear = await FinancialYearData.LoadFinancialYearByDateTime(item.TransactionDateTime)
			?? throw new Exception("No financial year found for the selected transaction date.");
		await FinancialYearData.ValidateFinancialYear(item.TransactionDateTime);
		item.FinancialYearId = financialYear.Id;

		if (!update)
		{
			item.TransactionNo = await GenerateCodes.GenerateTripRequestTransactionNo(item);
			item.RequestStatus = RequestStatus.Requested.ToString();
		}
		else
		{
			var existing = await CommonData.LoadTableDataById<TripRequestModel>(FleetNames.TripRequest, item.Id);
			item.TransactionNo = existing.TransactionNo;
		}
	}

	public static async Task<int> SaveTransaction(TripRequestModel tripRequest, bool showNotification = true)
	{
		var isUpdate = tripRequest.Id > 0;
		await ValidateTransaction(tripRequest, isUpdate);
		var previous = isUpdate
			? await CommonData.LoadTableDataById<TripRequestModel>(FleetNames.TripRequest, tripRequest.Id)
			: null;

		tripRequest.Id = await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertTripRequest(tripRequest, transaction);
			var current = await CommonData.LoadTableDataById<TripRequestModel>(FleetNames.TripRequest, id, transaction);
			var diff = AuditTrailData.GetDifference(previous, current);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = FleetNames.TripRequest,
				RecordNo = tripRequest.TransactionNo,
				RecordValue = isUpdate ? diff : null,
				CreatedBy = isUpdate ? tripRequest.LastModifiedBy.Value : tripRequest.CreatedBy,
				CreatedFromPlatform = isUpdate ? tripRequest.LastModifiedFromPlatform : tripRequest.CreatedFromPlatform
			}, transaction);
			return id;
		});

		if (showNotification)
			await TripRequestNotify.Notify(tripRequest.Id, isUpdate ? NotifyType.Updated : NotifyType.Created);

		return tripRequest.Id;
	}
}
