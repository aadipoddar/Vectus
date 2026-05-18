CREATE PROCEDURE [dbo].[Insert_HDR]
	@Id INT OUTPUT,
	@Name VARCHAR(250),
	@Code VARCHAR(10),
	@Remarks VARCHAR(MAX),
	@Status BIT = 1
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[HDR]
		(
			[Name],
			[Code],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@Name,
			@Code,
			@Remarks,
			@Status
		);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[HDR]
		SET
			[Name] = @Name,
			[Code] = @Code,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END