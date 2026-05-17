CREATE PROCEDURE [dbo].[Insert_VehicleDocument]
	@Id INT OUTPUT,
	@TransactionNo VARCHAR(100),
	@TransactionDateTime DATETIME,
	@VehicleDocumentTypeId INT,
	@VehicleId INT,
	@CurrentKM MONEY,
	@Rate MONEY,
	@RenewalDate DATETIME,
	@Remarks VARCHAR(MAX),
	@DocumentUrl VARCHAR(MAX) = NULL,
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
		INSERT INTO [dbo].[VehicleDocument]
		(
			[TransactionNo],
			[TransactionDateTime],
			[VehicleDocumentTypeId],
			[VehicleId],
			[CurrentKM],
			[Rate],
			[RenewalDate],
			[Remarks],
			[DocumentUrl],
			[CreatedBy],
			[CreatedFromPlatform],
			[Status]
		)
		VALUES
		(
			@TransactionNo,
			@TransactionDateTime,
			@VehicleDocumentTypeId,
			@VehicleId,
			@CurrentKM,
			@Rate,
			@RenewalDate,
			@Remarks,
			@DocumentUrl,
			@CreatedBy,
			@CreatedFromPlatform,
			@Status
		);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[VehicleDocument]
		SET
			[TransactionNo] = @TransactionNo,
			[TransactionDateTime] = @TransactionDateTime,
			[VehicleDocumentTypeId] = @VehicleDocumentTypeId,
			[VehicleId] = @VehicleId,
			[CurrentKM] = @CurrentKM,
			[Rate] = @Rate,
			[RenewalDate] = @RenewalDate,
			[Remarks] = @Remarks,
			[DocumentUrl] = @DocumentUrl,
			[Status] = @Status,
			[LastModifiedBy] = @LastModifiedBy,
			[LastModifiedAt] = @LastModifiedAt,
			[LastModifiedFromPlatform] = @LastModifiedFromPlatform
		WHERE
			[Id] = @Id;
	END

	SELECT @Id AS Id;
END