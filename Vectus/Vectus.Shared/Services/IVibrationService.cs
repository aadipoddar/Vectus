namespace Vectus.Shared.Services;

public interface IVibrationService
{
	public void VibrateHapticClick();
	public void VibrateHapticLongPress();
	public void VibrateWithTime(int milliseconds);
}
