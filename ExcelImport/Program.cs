using OfficeOpenXml;

using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.Route.Data;
using VectusLibrary.Fleet.Route.Models;
using VectusLibrary.Fleet.Vehicle.Models;

SqlDataAccess.SetupConfiguration();

FileInfo fileInfo = new(@"C:\Others\driver.xlsx");

ExcelPackage.License.SetNonCommercialPersonal("AadiSoft");

using var package = new ExcelPackage(fileInfo);

await package.LoadAsync(fileInfo);

var worksheet1 = package.Workbook.Worksheets[0];

await InsertVehicleDriver(worksheet1);

Console.WriteLine("Finished importing Items.");
Console.ReadLine();

#region Unused

/*

await InsertVehicles();

await ImportVehicles(worksheet1);

await ImportDrivers(worksheet1);

await InsertVehicles(worksheet1);

await InsertVehicleDriver(worksheet1);

static async Task ImportVehicles(ExcelWorksheet worksheet1)
{
	int row = 2;

	while (worksheet1.Cells[row, 1].Value != null)
	{
		var code = worksheet1.Cells[row, 1].Value.ToString();
		var shortCode = worksheet1.Cells[row, 2].Value.ToString();
		var chasis = worksheet1.Cells[row, 3].Value.ToString();
		var engine = worksheet1.Cells[row, 4].Value.ToString();
		var purchaseDateStr = worksheet1.Cells[row, 5].Value.ToString();
		var purchaseDate = DateTime.Parse(purchaseDateStr);
		var vehicleTypeId = worksheet1.Cells[row, 6].Value.ToString();
		var companyId = worksheet1.Cells[row, 7].Value.ToString();

		if (string.IsNullOrWhiteSpace(code) ||
			string.IsNullOrWhiteSpace(shortCode) ||
			string.IsNullOrWhiteSpace(chasis) ||
			string.IsNullOrWhiteSpace(engine) ||
			string.IsNullOrWhiteSpace(vehicleTypeId) ||
			string.IsNullOrWhiteSpace(companyId))
		{
			Console.WriteLine("Not Inserted Row = " + row);
			continue;
		}

		code = code.Trim().RemoveSpace();
		shortCode = shortCode.Trim().RemoveSpace();
		engine = engine.Trim().RemoveSpace();
		chasis = chasis.Trim().RemoveSpace();

		Console.WriteLine("Inserting New Vehicle: " + code);
		await VehicleData.InsertVehicle(new()
		{
			Id = 0,
			Code = code,
			ShortCode = shortCode,
			ChasisCode = chasis,
			EngineCode = engine,
			OpeningKM = 0,
			Remarks = null,
			PurchaseDate = purchaseDate,
			VehicleTypeId = int.Parse(vehicleTypeId),
			CompanyId = int.Parse(companyId),
			Status = true
		});

		row++;
	}
}

static async Task InsertVehicles()
{
	var vehicles = await VamosysApiService.GetLiveVehicles();

	foreach (var vehicle in vehicles)
	{
		Console.WriteLine("Inserting New Vehicle: " + vehicle.VehicleId);

		try
		{
			await VehicleData.SaveTransaction(new()
			{
				Id = 0,
				Code = vehicle.VehicleId,
				ShortCode = vehicle.VehicleId[^4..],
				OpeningKM = vehicle.OdometerKM,
				PurchaseDate = DateTime.Now,
				VehicleTypeId = 1,
				CompanyId = 1
			}, 1, "Import Script");
		}
		catch
		{
			Console.WriteLine("Error occurred while inserting vehicle: " + vehicle.VehicleId);
		}
	}
}

static async Task InsertDrivers(ExcelWorksheet worksheet1)
{
	int row = 2;
	while (worksheet1.Cells[row, 1].Value != null)
	{
		var mobile = worksheet1.Cells[row, 1].Value.ToString();
		var name = worksheet1.Cells[row, 2].Value.ToString();
		var license = worksheet1.Cells[row, 3].Value.ToString();

		if (string.IsNullOrWhiteSpace(mobile) ||
			string.IsNullOrWhiteSpace(license) ||
			string.IsNullOrWhiteSpace(name))
		{
			Console.WriteLine("Not Inserted Row = " + row);
			continue;
		}

		name = name.Trim();
		license = license.Trim();
		mobile = mobile.Trim();

		Console.WriteLine("Inserting New Driver: " + name);
		await DriverData.SaveTransaction(new()
		{
			Id = 0,
			Name = name,
			LicenseNo = license,
			Mobile = mobile,
			Status = true
		}, 1, "Import Script");
		row++;
	}
}

static async Task InsertVehicles(ExcelWorksheet worksheet1)
{
	int row = 2;
	while (worksheet1.Cells[row, 1].Value != null)
	{
		var code = worksheet1.Cells[row, 1].Value.ToString();
		var shortCode = worksheet1.Cells[row, 2].Value.ToString();
		var vehicleTypeId = worksheet1.Cells[row, 3].Value.ToString();
		var companyId = worksheet1.Cells[row, 4].Value.ToString();
		var sdrId = worksheet1.Cells[row, 5].Value.ToString();

		if (string.IsNullOrWhiteSpace(code) ||
			string.IsNullOrWhiteSpace(shortCode) ||
			string.IsNullOrWhiteSpace(vehicleTypeId) ||
			string.IsNullOrWhiteSpace(companyId) ||
			string.IsNullOrWhiteSpace(sdrId))
		{
			Console.WriteLine("Not Inserted Row = " + row);
			continue;
		}

		code = code.Trim();
		shortCode = shortCode.Trim();
		vehicleTypeId = vehicleTypeId.Trim();
		companyId = companyId.Trim();
		sdrId = sdrId.Trim();

		Console.WriteLine("Inserting New Vehicle: " + code);
		await VehicleData.SaveTransaction(new()
		{
			Id = 0,
			Code = code,
			ShortCode = shortCode,
			SDRId = int.Parse(sdrId),
			CompanyId = int.Parse(companyId),
			VehicleTypeId = int.Parse(vehicleTypeId),
			ChasisCode = null,
			EngineCode = null,
			PurchaseDate = DateTime.Now,
			OpeningKM = 0,
			Status = true
		}, 1, "Import Script");
		row++;
	}
}

static async Task InsertVehicleDriver(ExcelWorksheet worksheet1)
{
	int row = 2;
	while (worksheet1.Cells[row, 1].Value != null)
	{
		var code = worksheet1.Cells[row, 1].Value.ToString();
		var name = worksheet1.Cells[row, 2].Value.ToString();

		if (string.IsNullOrWhiteSpace(code) ||
			string.IsNullOrWhiteSpace(name))
		{
			Console.WriteLine("Not Inserted Row = " + row);
			continue;
		}

		code = code.Trim();
		name = name.Trim();

		var vehicles = await CommonData.LoadTableData<VehicleModel>(FleetNames.Vehicle);
		var drivers = await CommonData.LoadTableData<DriverModel>(FleetNames.Driver);

		var vehicle = vehicles.FirstOrDefault(v => string.Equals(v.ShortCode, code[^4..], StringComparison.OrdinalIgnoreCase));
		var driver = drivers.FirstOrDefault(d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase));

		Console.WriteLine("Inserting New Vehicle: " + code);
		await VehicleDriverData.SaveTransaction(new()
		{
			Id = 0,
			DriverId = driver.Id,
			StartDateTime = DateTime.Now,
			VehicleId = vehicle.Id
		}, 1, "Import Script");
		row++;
	}
}

*/
#endregion

static async Task InsertVehicleDriver(ExcelWorksheet worksheet1)
{
	int row = 2;
	while (worksheet1.Cells[row, 1].Value != null)
	{
		var code = worksheet1.Cells[row, 1].Value.ToString();
		var name = worksheet1.Cells[row, 2].Value.ToString();

		if (string.IsNullOrWhiteSpace(code) ||
			string.IsNullOrWhiteSpace(name))
		{
			Console.WriteLine("Not Inserted Row = " + row);
			continue;
		}

		code = code.Trim();
		name = name.Trim();

		var vehicles = await CommonData.LoadTableData<VehicleModel>(FleetNames.Vehicle);
		var drivers = await CommonData.LoadTableData<DriverModel>(FleetNames.Driver);

		var vehicle = vehicles.FirstOrDefault(v => string.Equals(v.ShortCode, code[^4..], StringComparison.OrdinalIgnoreCase));
		var driver = drivers.FirstOrDefault(d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase));

		Console.WriteLine("Inserting New Vehicle: " + code);
		await VehicleDriverData.SaveTransaction(new()
		{
			Id = 0,
			DriverId = driver.Id,
			StartDateTime = DateTime.Now,
			VehicleId = vehicle.Id
		}, 1, "Import Script");
		row++;
	}
}