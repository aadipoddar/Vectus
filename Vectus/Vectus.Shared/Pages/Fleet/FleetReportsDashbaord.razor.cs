using Vectus.Shared.Services;

using VectusLibrary.Operations.Models;

namespace Vectus.Shared.Pages.Fleet;

public partial class FleetReportsDashbaord
{
	private UserModel _user;
	private bool _isLoading = true;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, VibrationService, [UserRoles.Fleet, UserRoles.Reports]);

		_isLoading = false;
		StateHasChanged();
	}
}
