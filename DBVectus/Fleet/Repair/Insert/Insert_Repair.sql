CREATE PROCEDURE [dbo].[Insert_Repair]
	@Id INT OUTPUT,
	@TransactionNo VARCHAR(MAX),
	@CompanyId INT,
	@TransactionDateTime DATETIME,
	@FinancialYearId INT,
	@GarageId INT,
	@VehicleId INT,
	@CurrentKM MONEY,
	@GarageInDateTime DATETIME,
	@GarageOutDateTime DATETIME,
	@TotalItems INT,
	@TotalQuantity MONEY,
	@TotalAmount MONEY,
	@Remarks VARCHAR(MAX),
	@CreatedBy INT,
	@CreatedAt DATETIME,
	@CreatedFromPlatform VARCHAR(MAX),
	@Status BIT,
	@LastModifiedBy INT,
	@LastModifiedAt DATETIME,
	@LastModifiedFromPlatform VARCHAR(MAX)
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Repair]
		(
			[TransactionNo],
			[CompanyId],
			[TransactionDateTime],
			[FinancialYearId],
			[GarageId],
			[VehicleId],
			[CurrentKM],
			[GarageInDateTime],
			[GarageOutDateTime],
			[TotalItems],
			[TotalQuantity],
			[TotalAmount],
			[Remarks],
			[CreatedBy],
			[CreatedAt],
			[CreatedFromPlatform],
			[Status]
		) VALUES
		(
			@TransactionNo,
			@CompanyId,
			@TransactionDateTime,
			@FinancialYearId,
			@GarageId,
			@VehicleId,
			@CurrentKM,
			@GarageInDateTime,
			@GarageOutDateTime,
			@TotalItems,
			@TotalQuantity,
			@TotalAmount,
			@Remarks,
			@CreatedBy,
			@CreatedAt,
			@CreatedFromPlatform,
			@Status
		)

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Repair]
		SET
			[TransactionNo] = @TransactionNo,
			[CompanyId] = @CompanyId,
			[TransactionDateTime] = @TransactionDateTime,
			[FinancialYearId] = @FinancialYearId,
			[GarageId] = @GarageId,
			[VehicleId] = @VehicleId,
			[CurrentKM] = @CurrentKM,
			[GarageInDateTime] = @GarageInDateTime,
			[GarageOutDateTime] = @GarageOutDateTime,
			[TotalItems] = @TotalItems,
			[TotalQuantity] = @TotalQuantity,
			[TotalAmount] = @TotalAmount,
			[Remarks] = @Remarks,
			[Status] = @Status,
			[LastModifiedBy] = @LastModifiedBy,
			[LastModifiedAt] = @LastModifiedAt,
			[LastModifiedFromPlatform] = @LastModifiedFromPlatform
		WHERE
			[Id] = @Id
	END

	SELECT @Id AS Id
END
