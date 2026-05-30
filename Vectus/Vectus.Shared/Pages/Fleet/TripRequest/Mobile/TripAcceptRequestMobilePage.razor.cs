using Vectus.Shared.Components.Dialog;

using VectusLibrary.Fleet.TripRequest.Data;
using VectusLibrary.Fleet.TripRequest.Models;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Operations.Models;

namespace Vectus.Shared.Pages.Fleet.TripRequest.Mobile;

public partial class TripAcceptRequestMobilePage
{
	private UserModel _user;
	private SDRModel _mySDR;
	private bool _isLoading = true;
	private bool _isProcessing;

	private List<TripRequestOverviewModel> _requests = [];
	private int _activeId;

	private string _confirmMode; // null | "accept" | "decline"
	private string _remarks = string.Empty;

	private string _overlayKind; // null | "accept" | "decline"
	private string _overlayNum = string.Empty;
	private string _overlayRemarks = string.Empty;

	private DateTime _currentDateTime;

	private ToastNotification _toastNotification;

	private TripRequestOverviewModel Active => _requests.FirstOrDefault(r => r.Id == _activeId) ?? _requests.FirstOrDefault();
	private List<TripRequestOverviewModel> Queue => [.. _requests.Where(r => r.Id != Active?.Id)];

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, VibrationService, [UserRoles.Fleet]);
			await LoadData();

			_isLoading = false;
			StateHasChanged();
		}
		catch { NavigationManager.NavigateTo(PageRouteNames.Dashboard); }
	}

	private async Task LoadData()
	{
		_currentDateTime = await CommonData.LoadCurrentDateTime();

		var sdrs = await CommonData.LoadTableDataByStatus<SDRModel>(FleetNames.SDR);
		_mySDR = sdrs.FirstOrDefault(s => s.UserId == _user.Id);

		_requests = _mySDR is not null ? await TripRequestData.LoadBySDRRequestStatus(_mySDR.Id, nameof(RequestStatus.Requested)) : [];

		if (_requests.All(r => r.Id != _activeId))
			_activeId = _requests.FirstOrDefault()?.Id ?? 0;
	}
	#endregion

	#region Changed Events
	private void OpenConfirm(string mode)
	{
		if (Active is null)
			return;

		_confirmMode = mode;
		_remarks = string.Empty;
	}
	#endregion

	#region Actions
	private async Task ConfirmTransaction()
	{
		var mode = _confirmMode;
		var active = Active;
		_confirmMode = null;

		if (active is null || mode is null || _isProcessing)
			return;

		var accept = mode == "accept";

		try
		{
			_isProcessing = true;
			StateHasChanged();

			var tripRequest = await CommonData.LoadTableDataById<TripRequestModel>(FleetNames.TripRequest, active.Id)
				?? throw new Exception("Transaction not found.");

			if (!tripRequest.Status)
				throw new InvalidOperationException("Cannot respond to a deleted request.");

			if (_mySDR is null || active.SDRId != _mySDR.Id)
				throw new UnauthorizedAccessException("This request is not assigned to you.");

			if (!string.Equals(tripRequest.RequestStatus, RequestStatus.Requested.ToString(), StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("Request is already Accepted or Rejected.");

			tripRequest.RequestStatus = (accept ? RequestStatus.Accepted : RequestStatus.Rejected).ToString();
			tripRequest.Remarks = _remarks;
			tripRequest.LastModifiedBy = _user.Id;
			tripRequest.LastModifiedAt = await CommonData.LoadCurrentDateTime();
			tripRequest.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

			await TripRequestData.SaveTransaction(tripRequest, false);

			_overlayKind = mode;
			_overlayNum = tripRequest.TransactionNo;
			_overlayRemarks = _remarks ?? string.Empty;

			await LoadData();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync($"Error While {(accept ? "Accepting" : "Declining")}", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Utilities
	private string AgoText(DateTime when)
	{
		var seconds = Math.Max(0, (_currentDateTime - when).TotalSeconds);
		return seconds < 60 ? "just now"
			: seconds < 3600 ? $"{Math.Round(seconds / 60)} min ago"
			: seconds < 86400 ? $"{Math.Round(seconds / 3600)} h ago"
			: $"{Math.Round(seconds / 86400)} d ago";
	}
	#endregion
}
