CREATE PROCEDURE [dbo].[Check_Repair_Overlap]
	@Id INT,
	@VehicleId INT,
	@GarageInDateTime DATETIME,
	@GarageOutDateTime DATETIME = NULL
AS
BEGIN
	SET NOCOUNT ON;

	-- Returns the first active repair where the same vehicle's garage period overlaps the
	-- candidate period. A null garage-out is open-ended (vehicle still in the garage), so it
	-- is treated as the maximum date. Two periods overlap when each starts before the other ends.
	SELECT TOP 1 *
	FROM [dbo].[Repair]
	WHERE [Id] <> @Id
		AND [VehicleId] = @VehicleId
		AND [Status] = 1
		AND @GarageInDateTime < ISNULL([GarageOutDateTime], '9999-12-31')
		AND [GarageInDateTime] < ISNULL(@GarageOutDateTime, '9999-12-31')
	ORDER BY [GarageInDateTime];
END
