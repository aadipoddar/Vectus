using Microsoft.Extensions.Caching.Memory;

using MudBlazor;

using VectusLibrary.Fleet.Repair.Models;
using VectusLibrary.Fleet.TripRequest.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace Vectus.Shared.Components.Dashboard;

public partial class DashboardChart
{
	private int _cacheHours = 12;

	private List<TripRequestOverviewModel> _tripRequests = [];
	private List<RepairOverviewModel> _repairs = [];
	private List<RepairJobOverviewModel> _repairJobs = [];

	// Row 1 - Trip Volume line / Repair Spend bar
	private List<ChartSeries<double>> _tripVolumeSeries = [];
	private List<ChartSeries<double>> _repairSpendSeries = [];
	private string[] _labels = [];

	// Row 2 - Top vehicles bar / Job breakdown donut
	private List<ChartSeries<double>> _vehicleRepairSeries = [];
	private string[] _vehicleLabels = [];

	private List<ChartSeries<double>> _jobBreakdownSeries = [];
	private string[] _jobBreakdownLabels = [];

	private readonly LineChartOptions _tripVolumeOptions = new()
	{
		ChartPalette = ["#2563eb"],
		YAxisFormat = "N0",
		ShowLegend = false,
		ShowDataMarkers = true,
		LineStrokeWidth = 2.5,
		InterpolationOption = InterpolationOption.NaturalSpline,
	};

	private readonly BarChartOptions _repairSpendOptions = new()
	{
		ChartPalette = ["#dc2626"],
		YAxisFormat = "N0",
		ShowLegend = false,
		YAxisLines = true,
		XAxisLines = false,
	};

	private readonly BarChartOptions _vehicleRepairOptions = new()
	{
		ChartPalette = ["#f59e0b"],
		YAxisFormat = "N0",
		ShowLegend = false,
		YAxisLines = true,
		XAxisLines = false,
	};

	private readonly PieChartOptions _jobBreakdownOptions = new()
	{
		ChartPalette = ["#2563eb", "#16a34a", "#f59e0b", "#dc2626", "#8b5cf6", "#0891b2", "#db2777", "#65a30d"],
		ShowValues = true,
	};

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await LoadData();
	}

	private async Task LoadData()
	{
		if (LoadCached())
			return;

		await LoadFresh();

		var expiry = TimeSpan.FromHours(_cacheHours);
		MemoryCache.Set(StorageFileNames.TripRequestsYearOverviewDataFileName, _tripRequests, expiry);
		MemoryCache.Set(StorageFileNames.RepairsYearOverviewDataFileName, _repairs, expiry);
		MemoryCache.Set(StorageFileNames.RepairJobsYearOverviewDataFileName, _repairJobs, expiry);
	}

	private bool LoadCached()
	{
		if (!MemoryCache.TryGetValue(StorageFileNames.TripRequestsYearOverviewDataFileName, out List<TripRequestOverviewModel> tripRequests))
			return false;

		_tripRequests = tripRequests ?? [];
		_repairs = MemoryCache.Get<List<RepairOverviewModel>>(StorageFileNames.RepairsYearOverviewDataFileName) ?? [];
		_repairJobs = MemoryCache.Get<List<RepairJobOverviewModel>>(StorageFileNames.RepairJobsYearOverviewDataFileName) ?? [];

		BuildAll();
		StateHasChanged();
		return true;
	}

	private async Task LoadFresh()
	{
		try
		{
			var cacheSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.AnalysisCacheHours);
			_cacheHours = int.TryParse(cacheSetting?.Value, out var hours) && hours > 0 ? hours : 12;

			// Window: first day of month 11 months ago → end of current month (12 months total).
			var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
			var windowStart = thisMonthStart.AddMonths(-11);
			var windowEnd = thisMonthStart.AddMonths(1).AddSeconds(-1);

			_tripRequests = await CommonData.LoadTableDataByDate<TripRequestOverviewModel>(FleetNames.TripRequestOverview, windowStart, windowEnd);
			_repairs = await CommonData.LoadTableDataByDate<RepairOverviewModel>(FleetNames.RepairOverview, windowStart, windowEnd);
			_repairJobs = await CommonData.LoadTableDataByDate<RepairJobOverviewModel>(FleetNames.RepairJobOverview, windowStart, windowEnd);

			_tripRequests = [.. _tripRequests.Where(_ => _.Status)];
			_repairs = [.. _repairs.Where(_ => _.Status)];
			_repairJobs = [.. _repairJobs.Where(_ => _.MasterStatus)];

			BuildAll();
		}
		catch { }
		finally { StateHasChanged(); }
	}

	private void BuildAll()
	{
		BuildMonthlyTrend();
		BuildTopVehicles();
		BuildJobBreakdown();
	}

	private void BuildMonthlyTrend()
	{
		var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

		var buckets = Enumerable.Range(0, 12)
			.Select(i => thisMonthStart.AddMonths(-11 + i))
			.ToList();

		_labels = [.. buckets.Select(b => b.ToString("MMM"))];

		var trips = new double[12];
		var spend = new double[12];

		for (int i = 0; i < 12; i++)
		{
			var monthStart = buckets[i];
			var monthEnd = monthStart.AddMonths(1);

			// Trip volume = count of accepted requests in the month.
			trips[i] = _tripRequests
				.Where(t => t.RequestStatus == nameof(RequestStatus.Accepted)
					&& t.TransactionDateTime >= monthStart
					&& t.TransactionDateTime < monthEnd)
				.Count();

			spend[i] = (double)_repairs
				.Where(r => r.TransactionDateTime >= monthStart && r.TransactionDateTime < monthEnd)
				.Sum(r => r.TotalAmount);
		}

		_tripVolumeSeries = [new ChartSeries<double> { Name = "Trips", Data = trips }];
		_repairSpendSeries = [new ChartSeries<double> { Name = "Spend", Data = spend }];
	}

	private void BuildTopVehicles()
	{
		// Top 5 vehicles by total repair spend in the window.
		var perVehicle = _repairs
			.GroupBy(r => r.VehicleCode)
			.Select(g => new
			{
				Vehicle = g.Key,
				Spend = (double)g.Sum(r => r.TotalAmount),
			})
			.Where(x => x.Spend > 0)
			.OrderByDescending(x => x.Spend)
			.Take(5)
			.ToList();

		_vehicleLabels = [.. perVehicle.Select(v => v.Vehicle)];
		var spendData = perVehicle.Select(v => v.Spend).ToArray();
		_vehicleRepairSeries = perVehicle.Count == 0
			? []
			: [new ChartSeries<double> { Name = "Spend", Data = spendData }];
	}

	private void BuildJobBreakdown()
	{
		// Group repair-job line items by Job name and sum totals.
		var combined = _repairJobs
			.GroupBy(j => j.Job ?? "Other")
			.Select(g => new { Job = g.Key, Total = (double)g.Sum(j => j.Total) })
			.Where(x => x.Total > 0)
			.OrderByDescending(x => x.Total)
			.ToList();

		// Keep top 7 slices, bucket the rest into "Other" so the donut stays readable.
		const int topN = 7;
		var labels = new List<string>();
		var values = new List<double>();

		foreach (var item in combined.Take(topN))
		{
			labels.Add(item.Job);
			values.Add(item.Total);
		}

		if (combined.Count > topN)
		{
			labels.Add("Other");
			values.Add(combined.Skip(topN).Sum(x => x.Total));
		}

		_jobBreakdownLabels = [.. labels];
		_jobBreakdownSeries = values.Count == 0 ? [] : values.ToArray().AsChartDataSet();
	}
}
