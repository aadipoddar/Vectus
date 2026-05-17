using Vectus.Shared.Services;

using VectusLibrary.Operations.Models;

namespace Vectus.Shared.Pages.Operations;

public partial class OperationsDashboard
{
	private UserModel _user;
	private bool _isLoading = true;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, VibrationService, [UserRoles.Admin]);

		_isLoading = false;
		StateHasChanged();
	}
}