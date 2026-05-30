CREATE PROCEDURE [dbo].[Insert_SDR]
	@Id INT OUTPUT,
	@Name VARCHAR(250),
	@Code VARCHAR(10),
	@UserId INT,
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[SDR]
		(
			[Name],
			[Code],
			[UserId],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@Name,
			@Code,
			@UserId,
			@Remarks,
			@Status
		);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[SDR]
		SET
			[Name] = @Name,
			[Code] = @Code,
			[UserId] = @UserId,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END