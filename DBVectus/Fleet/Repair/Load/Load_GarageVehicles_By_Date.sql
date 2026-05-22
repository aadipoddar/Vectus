CREATE PROCEDURE [dbo].[Load_GarageVehicles_By_Date]
	@StartDate DATETIME,
	@EndDate DATETIME = NULL
AS
BEGIN
	SET NOCOUNT ON;

	IF @EndDate IS NULL
	BEGIN
		SELECT *
		FROM [dbo].[Repair_Overview]
		WHERE [Status] = 1
			AND [GarageInDateTime] <= @StartDate
			AND ([GarageOutDateTime] IS NULL OR [GarageOutDateTime] >= @StartDate)
		ORDER BY [GarageInDateTime];
	END

	ELSE
	BEGIN
		SELECT *
		FROM [dbo].[Repair_Overview]
		WHERE [Status] = 1
			AND [GarageInDateTime] >= @StartDate
			AND [GarageInDateTime] <= @EndDate
		ORDER BY [GarageInDateTime];
	END
END
