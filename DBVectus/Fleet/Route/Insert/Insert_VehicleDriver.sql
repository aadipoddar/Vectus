CREATE PROCEDURE [dbo].[Insert_VehicleDriver]
	@Id INT OUTPUT,
	@VehicleId INT,
	@DriverId INT,
	@StartDateTime DATETIME,
	@EndDateTime DATETIME,
	@Remarks VARCHAR(MAX)
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[VehicleDriver]
		(
			[VehicleId],
			[DriverId],
			[StartDateTime],
			[EndDateTime],
			[Remarks]
		)
		VALUES
		(
			@VehicleId,
			@DriverId,
			@StartDateTime,
			@EndDateTime,
			@Remarks
		);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[VehicleDriver]
		SET
			[VehicleId] = @VehicleId,
			[DriverId] = @DriverId,
			[StartDateTime] = @StartDateTime,
			[EndDateTime] = @EndDateTime,
			[Remarks] = @Remarks
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END