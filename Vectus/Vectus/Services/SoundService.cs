using Plugin.Maui.Audio;

using Vectus.Shared.Services;

namespace Vectus.Services;

public class SoundService : ISoundService
{
	public async Task PlaySound(string soundFileName) =>
		AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync(soundFileName)).Play();
}
