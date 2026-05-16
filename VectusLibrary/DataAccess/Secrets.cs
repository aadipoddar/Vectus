using Microsoft.Extensions.Configuration;

using System.Reflection;

namespace VectusLibrary.DataAccess;

public static partial class Secrets
{
	public static string DatabaseName => "Vectus";

	public static string AzureConnectionString = GetSecret(nameof(AzureConnectionString));
	public static string AzureTestingConnectionString = GetSecret(nameof(AzureTestingConnectionString));
	public static string LocalConnectionString = "Data Source=AADILAPIKIIT;Initial Catalog=Vectus;Integrated Security=True;Connect Timeout=300;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False";

	public static string AzureBlobStorageAccountName => "vectusstore";
	public static string AzureBlobStorageConnectionString = GetSecret(nameof(AzureBlobStorageConnectionString));
	public static string AzureBlobStorageAccountKey = GetSecret(nameof(AzureBlobStorageAccountKey));

	public static string SyncfusionLicense = GetSecret(nameof(SyncfusionLicense));

	public static string Email => "softaadi@gmail.com";
	public static string EmailPassword = GetSecret(nameof(EmailPassword));

	public static string ToEmail = "aadipoddarmail@gmail.com";
	public static string ToName => "Vectus";

	public static string OnlineFullLogoPath => "https://raw.githubusercontent.com/aadipoddar/Vectus/refs/heads/main/Vectus/Vectus.Web/wwwroot/images/logo_full.png";
	public static string AadiSoftWebsite => "https://aadisoft.vercel.app";
	public static string AppWebsite => "https://vectus.azurewebsites.net";
	private static string GetSecret(string key) =>
		new ConfigurationBuilder()
			.AddUserSecrets(Assembly.GetExecutingAssembly())
			.AddEnvironmentVariables()
			.Build()
			.GetSection(key).Value;
}