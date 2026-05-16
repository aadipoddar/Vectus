using Vectus.Shared.Services;

namespace Vectus.Web.Services;

public class FormFactor : IFormFactor
{
	public string GetFormFactor() => "Web";

	public string GetPlatform() => Environment.OSVersion.ToString();
}
