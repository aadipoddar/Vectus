using Vectus.Shared.Components.Dialog;

using VectusLibrary.Accounts.Masters.Data;
using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.Fleet.Route.Data;
using VectusLibrary.Fleet.Route.Models;
using VectusLibrary.Fleet.TripRequest.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace Vectus.Shared.Pages.Fleet.TripRequest.Mobile;

public partial class TripRequestMobilePage
{
	private UserModel _user;
	private bool _isLoading = true;

	private bool _routeSheet;
	private bool _coSheet;
	private string _routeQuery = string.Empty;

	private CompanyModel _selectedCompany;
	private RouteOverviewModel _selectedRoute;
	private FinancialYearModel _selectedFinancialYear = new();
	private TripRequestModel _tripRequest = new();

	private List<CompanyModel> _companies = [];
	private List<RouteOverviewModel> _routes = [];
	private List<RouteOverviewModel> _suggestions = [];

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, VibrationService, [UserRoles.Fleet]);
			await InitializePage();
		}
		catch { NavigationManager.NavigateTo(PageRouteNames.Dashboard); }
	}

	private async Task InitializePage()
	{
		await LoadData();
		await LoadSelections();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_companies = await CommonData.LoadTableDataByStatus<CompanyModel>(AccountNames.Company);
		_routes = await RouteData.LoadRouteOverview();

		_companies = [.. _companies.OrderBy(c => c.Name)];
		_routes = [.. _routes.OrderBy(r => r.FromLocationName)];
		_suggestions = [.. _routes.Take(4)];
	}

	private async Task LoadSelections()
	{
		var currentDateTime = await CommonData.LoadCurrentDateTime();
		_tripRequest = new() { Id = 0, TransactionDateTime = currentDateTime, Status = true };

		var lastTransaction = await CommonData.LoadLastTableData<TripRequestModel>(FleetNames.TripRequest);
		if (lastTransaction is not null)
			_tripRequest.TransactionDateTime = lastTransaction.TransactionDateTime;

		var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
		_selectedCompany = _companies.FirstOrDefault(c => c.Id == int.Parse(mainCompanyId.Value))
			?? _companies.FirstOrDefault();
		_selectedRoute = _routes.FirstOrDefault();

		_tripRequest.CompanyId = _selectedCompany?.Id ?? 0;
		_tripRequest.RouteId = _selectedRoute?.Id ?? 0;

		_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_tripRequest.TransactionDateTime);
		if (_selectedFinancialYear is not null)
			_tripRequest.FinancialYearId = _selectedFinancialYear.Id;
	}
	#endregion

	#region Changed Events
	private List<RouteOverviewModel> FilteredRoutes
	{
		get
		{
			if (string.IsNullOrWhiteSpace(_routeQuery?.Trim()))
				return _routes;

			return [.. _routes.Where(r =>
				$"{r.Code} {r.FromLocationName} {r.ToLocationName}".Contains(_routeQuery?.Trim(), StringComparison.OrdinalIgnoreCase))];
		}
	}

	private void SelectRoute(RouteOverviewModel route)
	{
		_selectedRoute = route;
		_tripRequest.RouteId = route?.Id ?? 0;
	}

	private void SelectRouteAndClose(RouteOverviewModel route)
	{
		SelectRoute(route);
		_routeSheet = false;
		_routeQuery = string.Empty;
	}

	private void SelectCompany(CompanyModel company)
	{
		_selectedCompany = company;
		_tripRequest.CompanyId = company?.Id ?? 0;
		_coSheet = false;
	}
	#endregion

	#region Actions
	private void ContinueToMap()
	{
		if (_selectedRoute is null)
			return;

		var company = _selectedCompany?.Id ?? 0;
		NavigationManager.NavigateTo($"{PageRouteNames.TripRequestMobileMap}/{_selectedRoute.Id}?company={company}");
	}
	#endregion
}
