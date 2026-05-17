using Microsoft.AspNetCore.Components;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Inputs;

using Vectus.Shared.Components.Dialog;
using Vectus.Shared.Components.Input;
using Vectus.Shared.Services;

using VectusLibrary.Common;
using VectusLibrary.DataAccess;
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
	private Stream _pendingDocumentStream;
	private string _pendingDocumentFileName;
	private string _documentUrlToDelete = string.Empty;
	private VehicleDocumentTypeModel _selectedVehicleDocumentType;
	private VehicleModel _selectedVehicle;

	private List<VehicleDocumentModel> _vehicleDocuments = [];
	private List<VehicleDocumentTypeModel> _vehicleDocumentTypes = [];
	private List<VehicleModel> _vehicles = [];
	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditSelectedItem", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecoverSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<VehicleDocumentModel> _sfGrid;
	private DeleteConfirmationDialog _deleteConfirmationDialog;
	private RecoverConfirmationDialog _recoverConfirmationDialog;
	private CustomTextField _sfFirstFocus;

	private int _deleteTransactionId = 0;
	private string _deleteTransactionNo = string.Empty;

	private int _recoverTransactionId = 0;
	private string _recoverTransactionNo = string.Empty;
	private ToastNotification _toastNotification;

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
		catch
		{
			ResetPage();
		}
	}

	private async Task LoadData()
	{
		_vehicleDocuments = await CommonData.LoadTableData<VehicleDocumentModel>(FleetNames.VehicleDocument);
		_vehicleDocumentTypes = await CommonData.LoadTableDataByStatus<VehicleDocumentTypeModel>(FleetNames.VehicleDocumentType);
		_vehicles = await CommonData.LoadTableDataByStatus<VehicleModel>(FleetNames.Vehicle);

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

	#region Change Events
	private Task OnVehicleDocumentTypeChanged()
	{
		if (_selectedVehicleDocumentType is null || _selectedVehicleDocumentType.Id <= 0)
			return Task.CompletedTask;

		_vehicleDocument.VehicleDocumentTypeId = _selectedVehicleDocumentType.Id;
		_vehicleDocument.Rate = _selectedVehicleDocumentType.Rate;
		return Task.CompletedTask;
	}

	private Task OnVehicleChanged()
	{
		if (_selectedVehicle is null || _selectedVehicle.Id <= 0)
			return Task.CompletedTask;

		_vehicleDocument.VehicleId = _selectedVehicle.Id;
		return Task.CompletedTask;
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

			await VehicleDocumentData.SaveTransaction(
				_vehicleDocument,
				_pendingDocumentStream,
				_pendingDocumentFileName,
				_documentUrlToDelete);

			await _toastNotification.ShowAsync("Success", $"Vehicle Document transaction '{_vehicleDocument.TransactionNo}' has been saved successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.VehicleDocument, true);
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
	private async Task OnEditVehicleDocument(VehicleDocumentModel vehicleDocument)
	{
		_vehicleDocument = await CommonData.LoadTableDataById<VehicleDocumentModel>(FleetNames.VehicleDocument, vehicleDocument.Id)
			?? throw new Exception("Vehicle Document transaction not found for editing.");
		_pendingDocumentStream?.Dispose();
		_pendingDocumentStream = null;
		_pendingDocumentFileName = null;
		_documentUrlToDelete = string.Empty;
		_isUploadDialogVisible = false;
		StateHasChanged();
	}

	private async Task ConfirmDelete()
	{
		try
		{
			_isProcessing = true;
			await _deleteConfirmationDialog.HideAsync();

			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			var vehicleDocument = await CommonData.LoadTableDataById<VehicleDocumentModel>(FleetNames.VehicleDocument, _deleteTransactionId)
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
			_deleteTransactionId = 0;
			_deleteTransactionNo = string.Empty;
		}
	}

	private async Task ConfirmRecover()
	{
		try
		{
			_isProcessing = true;
			await _recoverConfirmationDialog.HideAsync();

			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			var vehicleDocument = await CommonData.LoadTableDataById<VehicleDocumentModel>(FleetNames.VehicleDocument, _recoverTransactionId)
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
			_recoverTransactionId = 0;
			_recoverTransactionNo = string.Empty;
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
		if (args.Files is null || args.Files.Count == 0 || args.Files[0].File is null)
			return;

		var file = args.Files[0].File;
		var ms = new MemoryStream();
		await file.OpenReadStream(maxAllowedSize: 52428800).CopyToAsync(ms);
		ms.Position = 0;

		_pendingDocumentStream?.Dispose();
		_pendingDocumentStream = ms;
		_pendingDocumentFileName = file.Name;

		await _toastNotification.ShowAsync("Document Selected", $"'{_pendingDocumentFileName}' will be uploaded when you save the transaction.", ToastType.Info);
	}

	private Task OnRemoveFile(RemovingEventArgs args)
	{
		_pendingDocumentStream?.Dispose();
		_pendingDocumentStream = null;
		_pendingDocumentFileName = null;
		return Task.CompletedTask;
	}

	private async Task DownloadExistingDocument()
	{
		try
		{
			if (string.IsNullOrWhiteSpace(_vehicleDocument.DocumentUrl))
				return;

			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var (stream, contentType) = await BlobStorageAccess.DownloadFileFromBlobStorage(_vehicleDocument.DocumentUrl);
			var fileName = _vehicleDocument.DocumentUrl.Split('/').Last();
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Exported", "The export has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Exporting", ex.Message, ToastType.Error);
		}
	}

	private async Task MarkDocumentForRemoval()
	{
		if (string.IsNullOrWhiteSpace(_vehicleDocument.DocumentUrl))
		{
			_pendingDocumentStream?.Dispose();
			_pendingDocumentStream = null;
			_pendingDocumentFileName = null;
			return;
		}

		_documentUrlToDelete = _vehicleDocument.DocumentUrl;
		_vehicleDocument.DocumentUrl = null;
		_pendingDocumentStream?.Dispose();
		_pendingDocumentStream = null;
		_pendingDocumentFileName = null;

		await _toastNotification.ShowAsync("Document Removed", "Document will be removed when you save the transaction.", ToastType.Info);
	}
	#endregion

	#region Utilities
	private async Task OnMenuSelected(Syncfusion.Blazor.Navigations.MenuEventArgs<Syncfusion.Blazor.Navigations.MenuItem> args)
	{
		switch (args.Item.Id)
		{
			case "NewTransaction":
				ResetPage();
				break;
			case "SaveTransaction":
				await SaveTransaction();
				break;
			case "UploadDocument":
				UploadDocument();
				break;
			case "ToggleDeleted":
				await ToggleDeleted();
				break;
			case "ExportExcel":
				await ExportExcel();
				break;
			case "ExportPdf":
				await ExportPdf();
				break;
			case "EditSelected":
				await EditSelectedItem();
				break;
			case "DeleteRecoverSelected":
				await DeleteRecoverSelectedItem();
				break;
		}
	}

	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<VehicleDocumentModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditSelectedItem":
				await EditSelectedItem();
				break;
			case "DeleteRecoverSelectedItem":
				await DeleteRecoverSelectedItem();
				break;
		}
	}

	private async Task EditSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		_vehicleDocument = await CommonData.LoadTableDataById<VehicleDocumentModel>(FleetNames.VehicleDocument, selectedRecords[0].Id);
		if (_vehicleDocument is null)
			await _toastNotification.ShowAsync("Error", "Selected Vehicle Document transaction not found for editing.", ToastType.Error);

		_selectedVehicleDocumentType = _vehicleDocumentTypes.FirstOrDefault(vdt => vdt.Id == _vehicleDocument.VehicleDocumentTypeId);
		_selectedVehicle = _vehicles.FirstOrDefault(v => v.Id == _vehicleDocument.VehicleId);

		StateHasChanged();

		await _sfFirstFocus.FocusAsync();
	}

	private async Task DeleteRecoverSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count > 0)
		{
			if (selectedRecords[0].Status)
				await ShowDeleteConfirmation(selectedRecords[0].Id, selectedRecords[0].TransactionNo);
			else
				await ShowRecoverConfirmation(selectedRecords[0].Id, selectedRecords[0].TransactionNo);
		}
	}

	private async Task ShowDeleteConfirmation(int id, string transactionNo)
	{
		_deleteTransactionId = id;
		_deleteTransactionNo = transactionNo;
		await _deleteConfirmationDialog.ShowAsync();
	}

	private async Task CancelDelete()
	{
		_deleteTransactionId = 0;
		_deleteTransactionNo = string.Empty;
		await _deleteConfirmationDialog.HideAsync();
	}

	private async Task ShowRecoverConfirmation(int id, string transactionNo)
	{
		_recoverTransactionId = id;
		_recoverTransactionNo = transactionNo;
		await _recoverConfirmationDialog.ShowAsync();
	}

	private async Task CancelRecover()
	{
		_recoverTransactionId = 0;
		_recoverTransactionNo = string.Empty;
		await _recoverConfirmationDialog.HideAsync();
	}

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await LoadData();
	}

	private void ResetPage() =>
		NavigationManager.NavigateTo(PageRouteNames.VehicleDocument, true);

	private void NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.FleetMastersDashboard, true);
	#endregion
}
