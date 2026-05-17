namespace VectusLibrary.Utils.MailUtils;

public static class MailTheme
{
	/// <summary>
	/// Solid, email-safe equivalent of the app.css <c>--app-bg</c> gradient
	/// (<c>linear-gradient(135deg, #faf8f4 0%, #f3efe7 100%)</c>). Gradients
	/// are unreliable on <c>body</c>/<c>table</c> across mail clients, so this
	/// is the visual midpoint of the two gradient stops.
	/// </summary>
	public const string PageBackground = "#f7f3ec";

	/// <summary>Lighter gradient stop — used for inset boxes (e.g. the code box).</summary>
	public const string SurfaceTint = "#f3efe7";

	/// <summary>Border tone that matches the warm <see cref="SurfaceTint"/>.</summary>
	public const string SurfaceBorder = "#d8cfbf";
}
