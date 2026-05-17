CREATE PROCEDURE [dbo].[Insert_Vehicle]
	@Id INT OUTPUT,
	@Code VARCHAR(250),
	@ShortCode VARCHAR(250),
	@ChasisCode VARCHAR(250),
	@EngineCode VARCHAR(250),
	@PurchaseDate DATETIME,
	@OpeningKM MONEY,
	@VehicleTypeId INT,
	@CompanyId INT,
	@HDRId INT,
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Vehicle]
		(
			[Code],
			[ShortCode],
			[ChasisCode],
			[EngineCode],
			[PurchaseDate],
			[OpeningKM],
			[VehicleTypeId],
			[CompanyId],
			[HDRId],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@Code,
			@ShortCode,
			@ChasisCode,
			@EngineCode,
			@PurchaseDate,
			@OpeningKM,
			@VehicleTypeId,
			@CompanyId,
			@HDRId,
			@Remarks,
			@Status
		);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Vehicle]
		SET
			[Code] = @Code,
			[ShortCode] = @ShortCode,
			[ChasisCode] = @ChasisCode,
			[EngineCode] = @EngineCode,
			[PurchaseDate] = @PurchaseDate,
			[OpeningKM] = @OpeningKM,
			[VehicleTypeId] = @VehicleTypeId,
			[CompanyId] = @CompanyId,
			[HDRId] = @HDRId,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id
END