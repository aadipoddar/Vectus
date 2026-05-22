using VectusLibrary.Accounts.Masters.Data;
using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.Repair.Exports;
using VectusLibrary.Fleet.Repair.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;
using VectusLibrary.Utils.ExportUtils;
using VectusLibrary.Utils.MailUtils;

namespace VectusLibrary.Fleet.Repair.Data;

public static class RepairData
{
	private static async Task<int> InsertRepair(RepairModel repair, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(FleetNames.InsertRepair, repair, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Repair.");

	private static async Task<int> InsertRepairJob(RepairJobModel job, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(FleetNames.InsertRepairJob, job, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Repair Job.");

	public static async Task<List<RepairOverviewModel>> LoadGarageVehiclesByDate(DateTime StartDate, DateTime? EndDate = null) =>
		await SqlDataAccess.LoadData<RepairOverviewModel, dynamic>(FleetNames.LoadGarageVehiclesByDate, new { StartDate, EndDate });

	public static async Task<RepairModel> CheckRepairOverlap(RepairModel repair) =>
		(await SqlDataAccess.LoadData<RepairModel, dynamic>(FleetNames.CheckRepairOverlap, new { repair.Id, repair.VehicleId, repair.GarageInDateTime, repair.GarageOutDateTime })).FirstOrDefault();

	public static List<RepairJobModel> ConvertCartToJobs(List<RepairJobCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new RepairJobModel
		{
			Id = 0,
			MasterId = masterId,
			Job = item.Job,
			Quantity = item.Quantity,
			Rate = item.Rate,
			Total = item.Total,
			Remarks = item.Remarks,
			Status = true
		})];

	public static async Task DeleteTransaction(RepairModel repair, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		if (sqlDataAccessTransaction is null)
		{
			await SqlDataAccessTransaction.Run(transaction => DeleteTransaction(repair, transaction));
			await RepairNotify.Notify(repair.Id, NotifyType.Deleted);
			return;
		}

		await FinancialYearData.ValidateFinancialYear(repair.TransactionDateTime, sqlDataAccessTransaction);

		repair.Status = false;
		await InsertRepair(repair, sqlDataAccessTransaction);

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = AuditTrailActionTypes.Delete.ToString(),
			TableName = FleetNames.Repair,
			RecordNo = repair.TransactionNo,
			CreatedBy = repair.LastModifiedBy.Value,
			CreatedFromPlatform = repair.LastModifiedFromPlatform
		}, sqlDataAccessTransaction);
	}

	public static async Task RecoverTransaction(RepairModel repair)
	{
		repair.Status = true;
		var jobs = await CommonData.LoadTableDataByMasterId<RepairJobModel>(FleetNames.RepairJob, repair.Id);

		await SaveTransaction(repair, jobs, recover: true);

		await RepairNotify.Notify(repair.Id, NotifyType.Recovered);
	}

	private static async Task<RepairModel> ValidateTransaction(RepairModel repair, bool update = false, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		repair.Remarks = string.IsNullOrWhiteSpace(repair.Remarks) ? null : repair.Remarks.Trim();

		if (repair.CompanyId <= 0)
			throw new InvalidOperationException("Please select a company for the transaction.");

		if (repair.GarageId <= 0)
			throw new InvalidOperationException("Please select a garage for the transaction.");

		if (repair.VehicleId <= 0)
			throw new InvalidOperationException("Please select a vehicle for the transaction.");

		if (repair.GarageOutDateTime is not null && repair.GarageOutDateTime < repair.GarageInDateTime)
			throw new InvalidOperationException("Garage-out date/time cannot be earlier than garage-in date/time.");

		var overlap = await CheckRepairOverlap(repair);
		if (overlap is not null)
			throw new InvalidOperationException($"This vehicle is already in a garage for an overlapping period (Transaction: {overlap.TransactionNo}, Garage In: {overlap.GarageInDateTime}, Garage Out: {(overlap.GarageOutDateTime.HasValue ? overlap.GarageOutDateTime.Value.ToString() : "Still in garage")}). Please adjust the garage-in/out dates.");

		if (repair.TotalAmount <= 0)
			throw new InvalidOperationException("Total amount must be greater than zero.");

		if (repair.TotalItems <= 0)
			throw new InvalidOperationException("There must be at least one repair job in the transaction.");

		if (!update)
			repair.TransactionNo = await GenerateCodes.GenerateRepairTransactionNo(repair, sqlDataAccessTransaction);

		await FinancialYearData.ValidateFinancialYear(repair.TransactionDateTime, sqlDataAccessTransaction);

		if (update)
		{
			var existingRepair = await CommonData.LoadTableDataById<RepairModel>(FleetNames.Repair, repair.Id, sqlDataAccessTransaction)
				?? throw new InvalidOperationException("The transaction to be updated does not exist.");

			await FinancialYearData.ValidateFinancialYear(existingRepair.TransactionDateTime, sqlDataAccessTransaction);

			var user = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, repair.LastModifiedBy.Value, sqlDataAccessTransaction);
			if (!user.Admin)
				throw new InvalidOperationException("Only admin users are allowed to modify transactions.");

			repair.TransactionNo = existingRepair.TransactionNo;
		}

		return repair;
	}

	private static void ValidateTransactionJobs(RepairModel repair, List<RepairJobModel> jobs)
	{
		if (jobs is null || jobs.Count == 0)
			throw new InvalidOperationException("The transaction must have at least one repair job.");

		if (jobs.Any(d => !d.Status))
			throw new InvalidOperationException("Repair jobs must be active.");

		if (jobs.Any(d => d.Quantity <= 0 || d.Rate < 0 || d.Total < 0))
			throw new InvalidOperationException("Repair jobs must have a positive quantity and non-negative rate/total.");

		if (jobs.Any(d => string.IsNullOrWhiteSpace(d.Job)))
			throw new InvalidOperationException("Every repair job must have a description.");

		if (jobs.Count != repair.TotalItems)
			throw new InvalidOperationException("The number of repair jobs does not match the transaction summary.");

		if (jobs.Sum(d => d.Quantity) != repair.TotalQuantity)
			throw new InvalidOperationException("The total quantity of repair jobs does not match the transaction summary.");

		if (jobs.Sum(d => d.Total) != repair.TotalAmount)
			throw new InvalidOperationException("The total amount of repair jobs does not match the transaction summary.");

		foreach (var item in jobs)
		{
			item.Job = item.Job.Trim();
			item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		}
	}

	public static async Task<int> SaveTransaction(
		RepairModel repair,
		List<RepairJobModel> jobs,
		bool recover = false,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		bool update = repair.Id > 0;

		if (sqlDataAccessTransaction is null)
		{
			(MemoryStream, string)? previousInvoice = update && !recover ? await RepairInvoiceExport.ExportInvoice(repair.Id, InvoiceExportType.PDF) : null;

			repair.Id = await SqlDataAccessTransaction.Run(transaction => SaveTransaction(repair, jobs, recover, transaction));

			if (!recover)
				await RepairNotify.Notify(repair.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

			return repair.Id;
		}

		repair = await ValidateTransaction(repair, update, sqlDataAccessTransaction);
		ValidateTransactionJobs(repair, jobs);

		var previousRepair = update && !recover ? await CommonData.LoadTableDataById<RepairOverviewModel>(FleetNames.RepairOverview, repair.Id, sqlDataAccessTransaction) : null;
		var previousJobs = update && !recover ? await CommonData.LoadTableDataByMasterId<RepairJobOverviewModel>(FleetNames.RepairJobOverview, repair.Id, sqlDataAccessTransaction) : null;

		repair.Id = await InsertRepair(repair, sqlDataAccessTransaction);
		await SaveTransactionJobDetails(repair, jobs, update, sqlDataAccessTransaction);
		await SaveAuditTrail(repair, update, recover, previousRepair, previousJobs, sqlDataAccessTransaction);

		return repair.Id;
	}

	private static async Task SaveTransactionJobDetails(RepairModel repair, List<RepairJobModel> jobs, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
		{
			var existingJobs = await CommonData.LoadTableDataByMasterId<RepairJobModel>(FleetNames.RepairJob, repair.Id, sqlDataAccessTransaction);
			foreach (var item in existingJobs)
			{
				item.Status = false;
				await InsertRepairJob(item, sqlDataAccessTransaction);
			}
		}

		foreach (var item in jobs)
		{
			item.MasterId = repair.Id;
			await InsertRepairJob(item, sqlDataAccessTransaction);
		}
	}

	private static async Task SaveAuditTrail(
		RepairModel repair,
		bool update,
		bool recover,
		RepairOverviewModel previousRepair = null,
		List<RepairJobOverviewModel> previousJobs = null,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		string difference = null;

		if (update && !recover)
		{
			var currentRepair = await CommonData.LoadTableDataById<RepairOverviewModel>(FleetNames.RepairOverview, repair.Id, sqlDataAccessTransaction);
			var currentJobs = await CommonData.LoadTableDataByMasterId<RepairJobOverviewModel>(FleetNames.RepairJobOverview, repair.Id, sqlDataAccessTransaction);

			var repairDiff = AuditTrailData.GetDifference(previousRepair, currentRepair);
			var jobDiff = AuditTrailData.GetDifference(previousJobs, currentJobs, typeof(RepairJobOverviewModel));

			difference = AuditTrailData.CombineDifferences(
				(null, repairDiff),
				("Jobs", jobDiff));
		}

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = recover ? AuditTrailActionTypes.Recover.ToString() : update ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
			TableName = FleetNames.Repair,
			RecordNo = repair.TransactionNo,
			RecordValue = difference,
			CreatedBy = update ? repair.LastModifiedBy.Value : repair.CreatedBy,
			CreatedFromPlatform = update ? repair.LastModifiedFromPlatform : repair.CreatedFromPlatform
		}, sqlDataAccessTransaction);
	}
}
