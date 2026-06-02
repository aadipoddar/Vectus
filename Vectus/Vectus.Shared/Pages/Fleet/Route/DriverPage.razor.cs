using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Inputs;

using Vectus.Shared.Components.Dialog;
using Vectus.Shared.Components.Input;

using VectusLibrary.Fleet.Route.Data;
using VectusLibrary.Fleet.Route.Exports;
using VectusLibrary.Fleet.Route.Models;
using VectusLibrary.Operations.Models;
using VectusLibrary.Utils.ExportUtils;

namespace Vectus.Shared.Pages.Fleet.Route;

public partial class DriverPage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;
	private bool _isUploadDialogVisible = false;

	private DriverModel _driver = new();

	private List<DriverModel> _drivers = [];
	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditSelectedItem", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Download License (Alt + D)", Id = "DownloadSelectedLicense", IconCss = "e-icons e-download", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecoverSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<DriverModel> _sfGrid;
	private CustomTextField _sfFirstFocus;
	private ToastNotification _toastNotification;
	private ConfirmationDialog _confirmationDialog;

	private string _confirmTitle = string.Empty;
	private string _confirmMessage = string.Empty;
	private Func<Task> _confirmAction;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, VibrationService, [UserRoles.Fleet]);
			await LoadData();
		}
		catch { NavigationManager.NavigateTo(PageRouteNames.Dashboard); }
	}

	private async Task LoadData()
	{
		_drivers = await CommonData.LoadTableData<DriverModel>(FleetNames.Driver);

		if (!_showDeleted)
			_drivers = [.. _drivers.Where(vd => vd.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		_isLoading = false;
		StateHasChanged();

		if (_sfFirstFocus is not null)
			await _sfFirstFocus.FocusAsync();
	}
	#endregion

	#region Saving
	private async Task SaveTransaction()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			await _toastNotification.ShowAsync("Processing", "Please wait while the transaction is being saved...", ToastType.Info);

			await DriverData.SaveTransaction(_driver, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Saved", "Transaction has been saved successfully.", ToastType.Success);
			ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Saving", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}
	#endregion

	#region Actions
	private async Task EditSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		_driver = await CommonData.LoadTableDataById<DriverModel>(FleetNames.Driver, selectedRecords[0].Id);
		if (_driver is null)
		{
			await _toastNotification.ShowAsync("Error while Editing", "Transaction Not Found.", ToastType.Error);
			return;
		}

		_isUploadDialogVisible = false;
		StateHasChanged();
		await _sfFirstFocus.FocusAsync();
	}

	private async Task DeleteRecoverTransaction(int id, bool isRecover)
	{
		try
		{
			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing", $"{(isRecover ? "Recovering" : "Deleting")} transaction...", ToastType.Info);

			var driver = await CommonData.LoadTableDataById<DriverModel>(FleetNames.Driver, id)
				?? throw new Exception("Transaction not found.");

			if (isRecover) await DriverData.RecoverTransaction(driver, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());
			else await DriverData.DeleteTransaction(driver, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Success", $"Transaction {driver.Name} has been {(isRecover ? "recovered" : "deleted")} successfully.", ToastType.Success);
			ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while {(isRecover ? "recovering" : "deleting")} transaction: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task DeleteRecoverSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		var record = selectedRecords[0];

		await ShowConfirmation(record.Status ? "Delete" : "Recover",
			$"Are you sure you want to {(record.Status ? "delete" : "recover")} transaction {record.Name}",
			() => DeleteRecoverTransaction(record.Id, !record.Status));
	}

	private async Task ShowConfirmation(string title, string message, Func<Task> action)
	{
		_confirmTitle = title;
		_confirmMessage = message;
		_confirmAction = action;
		StateHasChanged();
		await _confirmationDialog.ShowAsync();
	}

	private async Task OnConfirmed()
	{
		await _confirmationDialog.HideAsync();
		if (_confirmAction is not null)
			await _confirmAction();
		_confirmAction = null;
	}

	private async Task OnCancelled()
	{
		_confirmAction = null;
		await _confirmationDialog.HideAsync();
	}
	#endregion

	#region Exporting
	private async Task ExportMaster(bool isExcel = false)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var (stream, fileName) = await DriverExport.ExportMaster(_drivers, isExcel ? ReportExportType.Excel : ReportExportType.PDF);
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Exported", "The export has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Exporting", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Uploading License
	private void UploadLicense()
	{
		if (_isProcessing)
			return;

		_isUploadDialogVisible = true;
		StateHasChanged();
	}

	private void CloseUploadDialog()
	{
		_isUploadDialogVisible = false;
		StateHasChanged();
	}

	private async Task OnUploaderFileChange(UploadChangeEventArgs args)
	{
		try
		{
			if (args.Files is null || args.Files.Count != 1)
				return;

			if (!string.IsNullOrEmpty(_driver.LicenseUrl))
				await RemoveExistingLicense();

			await using var file = args.Files[0].File.OpenReadStream(maxAllowedSize: 52428800);
			var fileName = $"{Guid.NewGuid()}_{args.Files[0].File.Name}";
			_driver.LicenseUrl = await BlobStorageAccess.UploadFileToBlobStorage(file, fileName, BlobStorageContainers.driverlicense);

			await _toastNotification.ShowAsync("Uploaded", "The license has been uploaded successfully.", ToastType.Success);
			StateHasChanged();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Uploading", ex.Message, ToastType.Error);
		}
	}

	private async Task OnRemoveFile(RemovingEventArgs args) =>
		await RemoveExistingLicense();

	private async Task RemoveExistingLicense()
	{
		try
		{
			if (string.IsNullOrEmpty(_driver.LicenseUrl))
				return;

			var fileName = _driver.LicenseUrl.Split('/').Last();
			await BlobStorageAccess.DeleteFileFromBlobStorage(fileName, BlobStorageContainers.driverlicense);
			_driver.LicenseUrl = null;

			await _toastNotification.ShowAsync("Removed", "The license has been removed successfully.", ToastType.Success);
			StateHasChanged();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Removing", ex.Message, ToastType.Error);
		}
	}

	private async Task DownloadExistingLicense()
	{
		try
		{
			if (string.IsNullOrWhiteSpace(_driver.LicenseUrl))
				return;

			var (stream, contentType) = await BlobStorageAccess.DownloadFileFromBlobStorage(_driver.LicenseUrl);
			var fileName = _driver.LicenseUrl.Split('/').Last();
			await SaveAndViewService.SaveAndView(fileName, stream);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Downloading", ex.Message, ToastType.Error);
		}
	}

	private async Task DownloadSelectedLicense()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		var licenseUrl = selectedRecords[0].LicenseUrl;
		if (string.IsNullOrWhiteSpace(licenseUrl))
		{
			await _toastNotification.ShowAsync("No License", "No license document is available for the selected driver.", ToastType.Warning);
			return;
		}

		try
		{
			await _toastNotification.ShowAsync("Processing", "Downloading the license...", ToastType.Info);

			var (stream, contentType) = await BlobStorageAccess.DownloadFileFromBlobStorage(licenseUrl);
			var fileName = licenseUrl.Split('/').Last();
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Downloaded", "The license has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Downloading", ex.Message, ToastType.Error);
		}
	}

	#endregion

	#region Utilities
	private async Task OnMenuSelected(Syncfusion.Blazor.Navigations.MenuEventArgs<Syncfusion.Blazor.Navigations.MenuItem> args)
	{
		switch (args.Item.Id)
		{
			case "NewTransaction": ResetPage(); break;
			case "SaveTransaction": await SaveTransaction(); break;
			case "UploadLicense": UploadLicense(); break;
			case "ToggleDeleted": await ToggleDeleted(); break;
			case "ExportExcel": await ExportMaster(true); break;
			case "ExportPdf": await ExportMaster(); break;
			case "EditSelectedItem": await EditSelectedItem(); break;
			case "DeleteRecoverSelectedItem": await DeleteRecoverSelectedItem(); break;
		}
	}

	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<DriverModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditSelectedItem": await EditSelectedItem(); break;
			case "DownloadSelectedLicense": await DownloadSelectedLicense(); break;
			case "DeleteRecoverSelectedItem": await DeleteRecoverSelectedItem(); break;
		}
	}

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await LoadData();
	}

	private void ResetPage() => PageRefresh.Request();
	#endregion
}
