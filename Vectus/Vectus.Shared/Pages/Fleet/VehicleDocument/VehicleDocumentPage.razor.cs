using Microsoft.AspNetCore.Components;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Inputs;

using Vectus.Shared.Components.Dialog;
using Vectus.Shared.Components.Input;

using VectusLibrary.Fleet.Vehicle.Data;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Fleet.VehicleDocument.Data;
using VectusLibrary.Fleet.VehicleDocument.Exports;
using VectusLibrary.Fleet.VehicleDocument.Models;
using VectusLibrary.Operations.Models;
using VectusLibrary.Utils.ExportUtils;

namespace Vectus.Shared.Pages.Fleet.VehicleDocument;

public partial class VehicleDocumentPage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;
	private bool _isUploadDialogVisible = false;

	private VehicleDocumentModel _vehicleDocument = new() { TransactionDateTime = DateTime.Now, RenewalDate = DateTime.Now.AddYears(1) };
	private VehicleDocumentTypeModel _selectedVehicleDocumentType;
	private VehicleLocationModel _selectedVehicle;

	private List<VehicleDocumentModel> _vehicleDocuments = [];
	private List<VehicleDocumentTypeModel> _vehicleDocumentTypes = [];
	private List<VehicleLocationModel> _vehicles = [];
	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditSelectedItem", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Download Document (Alt + D)", Id = "DownloadSelectedDocument", IconCss = "e-icons e-download", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecoverSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<VehicleDocumentModel> _sfGrid;
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
		_vehicleDocuments = await CommonData.LoadTableData<VehicleDocumentModel>(FleetNames.VehicleDocument);
		_vehicleDocumentTypes = await CommonData.LoadTableDataByStatus<VehicleDocumentTypeModel>(FleetNames.VehicleDocumentType);
		_vehicles = await VehicleData.LoadVehicleLocations();

		_vehicleDocumentTypes = [.. _vehicleDocumentTypes.OrderBy(vdt => vdt.Name)];
		_vehicles = [.. _vehicles.OrderBy(v => v.Code)];

		_selectedVehicleDocumentType = _vehicleDocumentTypes.FirstOrDefault(vdt => vdt.Id == _vehicleDocument.VehicleDocumentTypeId);
		_selectedVehicle = _vehicles.FirstOrDefault(v => v.Id == _vehicleDocument.VehicleId);

		if (!_showDeleted)
			_vehicleDocuments = [.. _vehicleDocuments.Where(vd => vd.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		_isLoading = false;
		StateHasChanged();

		if (_sfFirstFocus is not null)
			await _sfFirstFocus.FocusAsync();
	}
	#endregion

	#region Changed Events
	private async Task OnVehicleDocumentTypeChanged(VehicleDocumentTypeModel value)
	{
		if (value is null || value.Id == 0)
			return;

		_selectedVehicleDocumentType = value;
		_vehicleDocument.VehicleDocumentTypeId = _selectedVehicleDocumentType.Id;
		_vehicleDocument.Rate = _selectedVehicleDocumentType.Rate;
	}

	private async Task OnVehicleChanged(VehicleLocationModel value)
	{
		if (value is null || value.Id <= 0)
			return;

		_selectedVehicle = value;
		_vehicleDocument.VehicleId = _selectedVehicle.Id;
		_vehicleDocument.CurrentKM = _selectedVehicle.OdometerKM;
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
			await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

			var currentDateTime = await CommonData.LoadCurrentDateTime();
			_vehicleDocument.Status = true;
			_vehicleDocument.TransactionDateTime = DateOnly.FromDateTime(_vehicleDocument.TransactionDateTime).ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second));
			_vehicleDocument.CreatedBy = _user.Id;
			_vehicleDocument.LastModifiedBy = _user.Id;
			_vehicleDocument.CreatedAt = currentDateTime;
			_vehicleDocument.LastModifiedAt = currentDateTime;
			_vehicleDocument.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			_vehicleDocument.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

			await VehicleDocumentData.SaveTransaction(_vehicleDocument);

			await _toastNotification.ShowAsync("Success", $"Vehicle Document transaction '{_vehicleDocument.TransactionNo}' has been saved successfully.", ToastType.Success);
			ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Saving Transaction", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}
	#endregion

	#region Actions
	private async Task DeleteTransaction(int id)
	{
		try
		{
			_isProcessing = true;

			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			var vehicleDocument = await CommonData.LoadTableDataById<VehicleDocumentModel>(FleetNames.VehicleDocument, id)
				?? throw new Exception("Transaction not found.");

			var currentDateTime = await CommonData.LoadCurrentDateTime();

			vehicleDocument.Status = false;
			vehicleDocument.LastModifiedBy = _user.Id;
			vehicleDocument.LastModifiedAt = currentDateTime;
			vehicleDocument.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

			await VehicleDocumentData.DeleteTransaction(vehicleDocument);

			await _toastNotification.ShowAsync("Deleted", "Transaction has been deleted successfully.", ToastType.Success);
			ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Deleting", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private async Task RecoverTransaction(int id)
	{
		try
		{
			_isProcessing = true;

			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			var vehicleDocument = await CommonData.LoadTableDataById<VehicleDocumentModel>(FleetNames.VehicleDocument, id)
				?? throw new Exception("Transaction not found.");

			var currentDateTime = await CommonData.LoadCurrentDateTime();

			vehicleDocument.Status = true;
			vehicleDocument.LastModifiedBy = _user.Id;
			vehicleDocument.LastModifiedAt = currentDateTime;
			vehicleDocument.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

			await VehicleDocumentData.RecoverTransaction(vehicleDocument);

			await _toastNotification.ShowAsync("Recovered", "Transaction has been recovered successfully.", ToastType.Success);
			ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Recovering", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}
	#endregion

	#region Exporting
	private async Task ExportExcel()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var (stream, fileName) = await VehicleDocumentExport.ExportTransaction(_vehicleDocuments, ReportExportType.Excel);
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

	private async Task ExportPdf()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var (stream, fileName) = await VehicleDocumentExport.ExportTransaction(_vehicleDocuments, ReportExportType.PDF);
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

	#region Uploading Document
	private void UploadDocument()
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

			if (!string.IsNullOrEmpty(_vehicleDocument.DocumentUrl))
				await RemoveExistingDocument();

			await using var file = args.Files[0].File.OpenReadStream(maxAllowedSize: 52428800);
			var fileName = $"{Guid.NewGuid()}_{args.Files[0].File.Name}";
			_vehicleDocument.DocumentUrl = await BlobStorageAccess.UploadFileToBlobStorage(file, fileName, BlobStorageContainers.vehicledocument);

			await _toastNotification.ShowAsync("Uploaded", "The document has been uploaded successfully.", ToastType.Success);
			StateHasChanged();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Uploading", ex.Message, ToastType.Error);
		}
	}

	private async Task OnRemoveFile(RemovingEventArgs args) =>
		await RemoveExistingDocument();

	private async Task RemoveExistingDocument()
	{
		try
		{
			if (string.IsNullOrEmpty(_vehicleDocument.DocumentUrl))
				return;

			var fileName = _vehicleDocument.DocumentUrl.Split('/').Last();
			await BlobStorageAccess.DeleteFileFromBlobStorage(fileName, BlobStorageContainers.vehicledocument);
			_vehicleDocument.DocumentUrl = null;

			await _toastNotification.ShowAsync("Removed", "The document has been removed successfully.", ToastType.Success);
			StateHasChanged();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Removing", ex.Message, ToastType.Error);
		}
	}

	private async Task DownloadExistingDocument()
	{
		try
		{
			if (string.IsNullOrWhiteSpace(_vehicleDocument.DocumentUrl))
				return;

			var (stream, contentType) = await BlobStorageAccess.DownloadFileFromBlobStorage(_vehicleDocument.DocumentUrl);
			var fileName = _vehicleDocument.DocumentUrl.Split('/').Last();
			await SaveAndViewService.SaveAndView(fileName, stream);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Downloading", ex.Message, ToastType.Error);
		}
	}

	private async Task DownloadSelectedDocument()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		var documentUrl = selectedRecords[0].DocumentUrl;
		if (string.IsNullOrWhiteSpace(documentUrl))
		{
			await _toastNotification.ShowAsync("No Document", "No document is available for the selected transaction.", ToastType.Warning);
			return;
		}

		try
		{
			await _toastNotification.ShowAsync("Processing", "Downloading the document...", ToastType.Info);

			var (stream, contentType) = await BlobStorageAccess.DownloadFileFromBlobStorage(documentUrl);
			var fileName = documentUrl.Split('/').Last();
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Downloaded", "The document has been downloaded successfully.", ToastType.Success);
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
			case "UploadDocument": UploadDocument(); break;
			case "ToggleDeleted": await ToggleDeleted(); break;
			case "ExportExcel": await ExportExcel(); break;
			case "ExportPdf": await ExportPdf(); break;
			case "EditSelectedItem": await EditSelectedItem(); break;
			case "DeleteRecoverSelectedItem": await DeleteRecoverSelectedItem(); break;
		}
	}

	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<VehicleDocumentModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditSelectedItem": await EditSelectedItem(); break;
			case "DownloadSelectedDocument": await DownloadSelectedDocument(); break;
			case "DeleteRecoverSelectedItem": await DeleteRecoverSelectedItem(); break;
		}
	}

	private async Task EditSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		_vehicleDocument = await CommonData.LoadTableDataById<VehicleDocumentModel>(FleetNames.VehicleDocument, selectedRecords[0].Id);
		if (_vehicleDocument is null)
		{
			await _toastNotification.ShowAsync("Error", "Selected Vehicle Document transaction not found for editing.", ToastType.Error);
			return;
		}

		_selectedVehicleDocumentType = _vehicleDocumentTypes.FirstOrDefault(vdt => vdt.Id == _vehicleDocument.VehicleDocumentTypeId);
		_selectedVehicle = _vehicles.FirstOrDefault(v => v.Id == _vehicleDocument.VehicleId);
		StateHasChanged();
		await _sfFirstFocus.FocusAsync();
	}

	private async Task DeleteRecoverSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		var record = selectedRecords[0];

		if (record.Status)
			await ShowConfirmation("Delete", $"Are you sure you want to delete {record.TransactionNo}", () => DeleteTransaction(record.Id));
		else
			await ShowConfirmation("Recover", $"Are you sure you want to recover {record.TransactionNo}", () => RecoverTransaction(record.Id));
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

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await LoadData();
	}

	private void ResetPage() => PageRefresh.Request();
	#endregion
}
