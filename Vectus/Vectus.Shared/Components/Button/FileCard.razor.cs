using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Vectus.Shared.Components.Button;

public partial class FileCard
{
	/// <summary>
	/// The title displayed on the file card
	/// </summary>
	[Parameter] public string Title { get; set; } = string.Empty;

	/// <summary>
	/// The description text displayed below the title
	/// </summary>
	[Parameter] public string Description { get; set; } = string.Empty;

	/// <summary>
	/// The SVG icon content to display on the file
	/// </summary>
	[Parameter] public RenderFragment? IconContent { get; set; }

	/// <summary>
	/// The click event callback
	/// </summary>
	[Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
}