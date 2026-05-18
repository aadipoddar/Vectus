CREATE PROCEDURE [dbo].[Insert_Location]
	@Id INT OUTPUT,
	@Name VARCHAR(250),
	@Code VARCHAR(10),
	@Latitude DECIMAL(13, 10),
	@Longitude DECIMAL(13, 10),
	@Remarks VARCHAR(MAX),
	@Status BIT = 1
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Location]
		(
			[Name],
			[Code],
			[Latitude],
			[Longitude],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@Name,
			@Code,
			@Latitude,
			@Longitude,
			@Remarks,
			@Status
		);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Location]
		SET
			[Name] = @Name,
			[Code] = @Code,
			[Latitude] = @Latitude,
			[Longitude] = @Longitude,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END
