namespace VectusLibrary.Common;

public static class StorageFileNames
{
	public static string UserDataFileName => "user_data.json";
	public static string UserDeviceIdDataFileName => "user_device_id_data.json";

	public static string FinancialAccountingDataFileName => "financial_accounting_data.json";
	public static string FinancialAccountingCartDataFileName => "financial_accounting_cart_data.json";

	public static string RepairDataFileName => "repair_data.json";
	public static string RepairJobCartDataFileName => "repair_job_cart_data.json";

	#region Dashboard Analysis
	public static string TripRequestsOverviewDataFileName => "trip_requests_overview_data.json";
	public static string PendingTripRequestsDataFileName => "pending_trip_requests_data.json";
	public static string RepairsOverviewDataFileName => "repairs_overview_data.json";
	public static string VehiclesDataFileName => "vehicles_data.json";
	public static string DueDocumentsDataFileName => "due_documents_data.json";
	public static string InGarageRepairsDataFileName => "in_garage_repairs_data.json";
	public static string IdleVehiclesDataFileName => "idle_vehicles_data.json";
	public static string TripRequestsYearOverviewDataFileName => "trip_requests_year_overview_data.json";
	public static string RepairsYearOverviewDataFileName => "repairs_year_overview_data.json";
	public static string RepairJobsYearOverviewDataFileName => "repair_jobs_year_overview_data.json";
	#endregion
}