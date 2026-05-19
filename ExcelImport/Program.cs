using VectusLibrary.DataAccess;

SqlDataAccess.SetupConfiguration();

//FileInfo fileInfo = new(@"C:\Others\vehicle.xlsx");

//ExcelPackage.License.SetNonCommercialPersonal("AadiSoft");

//using var package = new ExcelPackage(fileInfo);

//await package.LoadAsync(fileInfo);

//var worksheet1 = package.Workbook.Worksheets[0];


Console.WriteLine("Finished importing Items.");
Console.ReadLine();

#region Unused

/*

await InsertVehicles();

await ImportVehicles(worksheet1);

await ImportDrivers(worksheet1);

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
 
*/
#endregion
