using Microsoft.AspNetCore.Components;

using MudBlazor;

using Vectus.Shared.Services;

using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.Common;

namespace Vectus.Shared.Components.Input;

public partial class CustomDatePicker
{
	private MudDatePicker _datePicker;
	private bool _isFocused;

	private void OnFocusIn() => _isFocused = true;
	private void OnFocusOut() => _isFocused = false;

	[Parameter] public DateTime Value { get; set; }
	[Parameter] public EventCallback<DateTime> ValueChanged { get; set; }

	[Parameter] public FinancialYearModel FinancialYear { get; set; }

	[Parameter] public string Label { get; set; } = "Transaction Date";
	[Parameter] public bool Required { get; set; } = true;
	[Parameter] public bool ShowFinancialYear { get; set; } = true;
	[Parameter] public string AddNewRoute { get; set; } = PageRouteNames.FinancialYearMaster;

	private DateTime? DateValue => Value == default ? null : Value;

	public ValueTask FocusAsync() =>
		_datePicker is null ? ValueTask.CompletedTask : _datePicker.FocusAsync();

	private async Task OnDateChangedInternal(DateTime? date)
	{
		if (!date.HasValue)
			return;

		Value = date.Value;
		await ValueChanged.InvokeAsync(Value);
	}

	private async Task NavigateToAddNew()
	{
		if (AddNewRoute is not null)
			await AuthenticationService.NavigateToRoute(AddNewRoute, FormFactor, JSRuntime, NavigationManager);
	}
}
