using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;

namespace Vectus.Services;

public class NotificationService() : Shared.Services.INotificationService
{
	public async Task ShowLocalNotification(int id, string title, string subTitle, string description)
	{
		if (!await LocalNotificationCenter.Current.AreNotificationsEnabled())
			await LocalNotificationCenter.Current.RequestNotificationPermission();

		var request = new NotificationRequest
		{
			NotificationId = id,
			Title = title,
			Subtitle = subTitle,
			Description = description,
			Schedule = new NotificationRequestSchedule
			{
				NotifyTime = DateTime.Now.AddSeconds(5)
			}
		};

		await LocalNotificationCenter.Current.Show(request);
	}
}
