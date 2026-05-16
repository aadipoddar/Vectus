using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Vectus.Shared.Services;

namespace Vectus.Web.Services;

public class SoundService(IJSRuntime jsRuntime) : ISoundService
{
	[Inject] private IJSRuntime JSRuntime { get; set; } = jsRuntime;

	public async Task PlaySound(string soundFileName) =>
		await JSRuntime.InvokeVoidAsync("PlaySound", soundFileName);
}
