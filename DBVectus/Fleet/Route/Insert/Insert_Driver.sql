CREATE PROCEDURE [dbo].[Insert_Driver]
	@Id INT OUTPUT,
	@Name VARCHAR(MAX),
	@Mobile VARCHAR(10),
	@Code VARCHAR(10),
	@Remarks VARCHAR(MAX),
	@LicenseUrl VARCHAR(MAX),
	@Status BIT = 1
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Driver]
		(
			[Name],
			[Mobile],
			[Code],
			[Remarks],
			[LicenseUrl],
			[Status]
		)
		VALUES
		(
			@Name,
			@Mobile,
			@Code,
			@Remarks,
			@LicenseUrl,
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
			[Remarks] = @Remarks,
			[LicenseUrl] = @LicenseUrl,
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END