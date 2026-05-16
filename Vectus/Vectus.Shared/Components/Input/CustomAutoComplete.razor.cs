using Microsoft.AspNetCore.Components;

using MudBlazor;

using System.Reflection;

using Vectus.Shared.Services;

namespace Vectus.Shared.Components.Input;

public partial class CustomAutoComplete<T>
{
	private MudAutocomplete<T> _autocomplete;
	private PropertyInfo _fieldProperty;
	private Type _cachedType;
	private bool _isFocused;

	private void OnFocusIn() => _isFocused = true;
	private void OnFocusOut() => _isFocused = false;

	[Parameter] public T Value { get; set; }
	[Parameter] public EventCallback<T> ValueChanged { get; set; }

	[Parameter] public IEnumerable<T> DataSource { get; set; }
	[Parameter] public string Placeholder { get; set; } = "Select...";
	[Parameter] public string FieldValue { get; set; }
	[Parameter] public bool Disabled { get; set; } = false;

	[Parameter] public string Label { get; set; }
	[Parameter] public bool Required { get; set; } = true;
	[Parameter] public string AddNewRoute { get; set; }
	[Parameter] public string AddNewLabel { get; set; } = "New";

	[Parameter] public bool OpenOnFocus { get; set; } = true;

	private bool ShowAddNew => AddNewRoute is not null && !Disabled;

	public ValueTask FocusAsync() =>
		_autocomplete is null ? ValueTask.CompletedTask : _autocomplete.FocusAsync();

	private string DisplayText(T item)
	{
		if (item is null)
			return string.Empty;

		if (string.IsNullOrEmpty(FieldValue))
			return item.ToString();

		if (_cachedType != typeof(T))
		{
			_fieldProperty = typeof(T).GetProperty(FieldValue);
			_cachedType = typeof(T);
		}

		return _fieldProperty?.GetValue(item)?.ToString() ?? string.Empty;
	}

	private Task<IEnumerable<T>> SearchAsync(string searchText, CancellationToken token)
	{
		if (DataSource is null)
			return Task.FromResult<IEnumerable<T>>([]);

		if (string.IsNullOrWhiteSpace(searchText))
			return Task.FromResult(DataSource);

		return Task.FromResult(DataSource.Where(item =>
		{
			var display = DisplayText(item);
			return !string.IsNullOrEmpty(display) &&
				   display.Contains(searchText, StringComparison.OrdinalIgnoreCase);
		}));
	}

	private async Task OnValueChangedInternal(T value)
	{
		Value = value;
		await ValueChanged.InvokeAsync(value);
	}

	private async Task NavigateToAddNew()
	{
		if (AddNewRoute is not null)
			await AuthenticationService.NavigateToRoute(AddNewRoute, FormFactor, JSRuntime, NavigationManager);
	}
}
