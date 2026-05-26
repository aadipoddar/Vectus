CREATE PROCEDURE [dbo].[Insert_Driver]
	@Id INT OUTPUT,
	@Name VARCHAR(MAX),
	@Mobile VARCHAR(10),
	@Code VARCHAR(10),
	@LicenseUrl VARCHAR(MAX),
	@LicenseNo VARCHAR(MAX),
	@LicenseExpiryDateTime DATETIME,
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Driver]
		(
			[Name],
			[Mobile],
			[Code],
			[LicenseUrl],
			[LicenseNo],
			[LicenseExpiryDateTime],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@Name,
			@Mobile,
			@Code,
			@LicenseUrl,
			@LicenseNo,
			@LicenseExpiryDateTime,
			@Remarks,
			@Status
		);

		SET @Id = SCOPE_IDENTITY();
	END
	ELSE
	BEGIN
		UPDATE [dbo].[Driver]
		SET
			[Name] = @Name,
			[Mobile] = @Mobile,
			[Code] = @Code,
			[LicenseUrl] = @LicenseUrl,
			[LicenseNo] = @LicenseNo,
			[LicenseExpiryDateTime] = @LicenseExpiryDateTime,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END