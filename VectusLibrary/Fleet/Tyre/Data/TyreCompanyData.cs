using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.Tyre.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace VectusLibrary.Fleet.Tyre.Data;

public static class TyreCompanyData
{
	private static async Task<int> InsertTyreCompany(TyreCompanyModel tyreCompany, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(FleetNames.InsertTyreCompany, tyreCompany, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Tyre Company.");

	public static async Task DeleteTransaction(TyreCompanyModel tyreCompany, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			tyreCompany.Status = false;
			await InsertTyreCompany(tyreCompany, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = FleetNames.TyreCompany,
				RecordNo = tyreCompany.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(TyreCompanyModel tyreCompany, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			tyreCompany.Status = true;
			await InsertTyreCompany(tyreCompany, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = FleetNames.TyreCompany,
				RecordNo = tyreCompany.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(TyreCompanyModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Code = item.Code?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Tyre Company name is required. Please enter a valid tyre company name.");

		if (item.Id == 0)
			item.Code = await GenerateCodes.GenerateTyreCompanyCode();

		if (string.IsNullOrWhiteSpace(item.Code))
			throw new Exception("Tyre Company code is required. Please try again.");

		var allTyreCompanies = await CommonData.LoadTableData<TyreCompanyModel>(FleetNames.TyreCompany);

		var existingByName = allTyreCompanies.FirstOrDefault(x => x.Id != item.Id && x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"Tyre Company name '{item.Name}' already exists. Please choose a different name.");

		var existingByCode = allTyreCompanies.FirstOrDefault(x => x.Id != item.Id && x.Code.Equals(item.Code, StringComparison.OrdinalIgnoreCase));
		if (existingByCode is not null)
			throw new Exception($"Tyre Company code '{item.Code}' already exists. Please choose a different code.");
	}

	public static async Task<int> SaveTransaction(TyreCompanyModel tyreCompany, int userId, string platform)
	{
		await ValidateTransaction(tyreCompany);

		var isUpdate = tyreCompany.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<TyreCompanyModel>(FleetNames.TyreCompany, tyreCompany.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertTyreCompany(tyreCompany, transaction);
			var diff = AuditTrailData.GetDifference(previous, tyreCompany);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = FleetNames.TyreCompany,
				RecordNo = tyreCompany.Name,
				RecordValue = isUpdate ? diff : null,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
