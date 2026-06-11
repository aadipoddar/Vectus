namespace VectusLibrary.DataAccess;

public static partial class Secrets
{
	public static string DatabaseName = "Vectus";

	public static string AzureConnectionString;
	public static string AzureTestingConnectionString;
	public static string LocalConnectionString = "Data Source=AADILAPIKIIT;Initial Catalog=Vectus;Integrated Security=True;Connect Timeout=300;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False";

	public static string AzureBlobStorageAccountName = "vectusstore";
	public static string AzureBlobStorageConnectionString;
	public static string AzureBlobStorageAccountKey;

	public static string SyncfusionLicense;

	public static string Email = "softaadi@gmail.com";
	public static string EmailPassword;

	public static string ToName = "Vectus";

	public static string OpenRouteServiceApiKey;
	public static string GoogleMapsApiKey;
	public static string GoogleMapsMapId;

	public static string VamosysAPI;

	public static string OnlineFullLogoPath = "https://raw.githubusercontent.com/aadipoddar/Vectus/refs/heads/main/Vectus/Vectus.Web/wwwroot/images/logo_full.png";
	public static string AadiSoftWebsite = "https://aadisoft.vercel.app";
	public static string AppWebsite = "https://vectus.azurewebsites.net";
}