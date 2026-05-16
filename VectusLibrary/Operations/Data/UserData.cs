using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Operations.Models;

namespace VectusLibrary.Operations.Data;

public static class UserData
{
	private static async Task<int> InsertUser(UserModel userModel, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(OperationNames.InsertUser, userModel, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert User.");

	public static async Task<int> InsertUser(UserModel userModel) =>
		await InsertUser(userModel, null);

	public static async Task DeleteTransaction(UserModel user, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			user.Status = false;
			await InsertUser(user, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = OperationNames.User,
				RecordNo = user.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(UserModel user, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			user.Status = true;
			await InsertUser(user, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = OperationNames.User,
				RecordNo = user.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(UserModel user)
	{
		user.Name = user.Name?.Trim().ToUpper() ?? string.Empty;
		user.Phone = user.Phone?.Trim() ?? string.Empty;
		user.Email = user.Email?.Trim() ?? string.Empty;
		user.Password = user.Password?.Trim() ?? string.Empty;
		user.Remarks = user.Remarks?.Trim() ?? string.Empty;
		user.Status = true;

		if (string.IsNullOrWhiteSpace(user.Name))
			throw new Exception("User name is required. Please enter a valid name.");

		if (string.IsNullOrWhiteSpace(user.Phone))
			throw new Exception("Phone number is required. Please enter a valid 10-digit phone number.");

		if (user.Phone.Length != 10 || !user.Phone.All(char.IsDigit))
			throw new Exception("Phone number must be exactly 10 digits.");

		if (string.IsNullOrWhiteSpace(user.Password))
			throw new Exception("Password is required. Please enter a valid password.");

		if (string.IsNullOrWhiteSpace(user.Email))
			user.Email = null;

		if (string.IsNullOrWhiteSpace(user.Remarks))
			user.Remarks = null;

		if (user.Id == 0)
		{
			user.FailedAttempts = 0;
			user.CodeResends = 0;
			user.LastCode = null;
			user.LastCodeDeviceId = null;
			user.LastCodeDateTime = null;
		}

		var allUsers = await CommonData.LoadTableData<UserModel>(OperationNames.User);

		var existingByPhone = allUsers.FirstOrDefault(existingUser =>
			existingUser.Id != user.Id &&
			existingUser.Phone == user.Phone);

		if (existingByPhone is not null)
			throw new Exception($"Phone number '{user.Phone}' already exists. Please use a different phone number.");

		if (!string.IsNullOrWhiteSpace(user.Email))
		{
			var existingByEmail = allUsers.FirstOrDefault(existingUser =>
				existingUser.Id != user.Id &&
				!string.IsNullOrWhiteSpace(existingUser.Email) &&
				existingUser.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase));

			if (existingByEmail is not null)
				throw new Exception($"Email '{user.Email}' already exists. Please use a different email.");
		}
	}

	public static async Task<int> SaveTransaction(UserModel user, int userId, string platform)
	{
		await ValidateTransaction(user);

		var isUpdate = user.Id > 0;
		var previous = isUpdate 
			? await CommonData.LoadTableDataById<UserModel>(OperationNames.User, user.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertUser(user, transaction);
			var diff = AuditTrailData.GetDifference(previous, user);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = OperationNames.User,
				RecordNo = user.Name,
				RecordValue = diff,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}

	private static UserModel GetAuditUser(UserModel user)
	{
		if (user is null)
			return null;

		return new()
		{
			Id = user.Id,
			Name = user.Name,
			Phone = user.Phone,
			Email = user.Email,
			Accounts = user.Accounts,
			Fleet = user.Fleet,
			Reports = user.Reports,
			Admin = user.Admin,
			Remarks = user.Remarks,
			Status = user.Status,
			CodeResends = user.CodeResends,
			FailedAttempts = user.FailedAttempts
		};
	}

	public static async Task ResetInsertUser(UserModel user)
	{
		user.Status = true;
		user.FailedAttempts = 0;
		user.CodeResends = 0;
		user.LastCodeDateTime = null;
		user.LastCode = null;
		user.LastCodeDeviceId = null;

		await InsertUser(user);
	}
}
