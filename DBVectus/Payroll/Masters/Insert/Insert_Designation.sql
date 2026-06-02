CREATE PROCEDURE [dbo].[Insert_Designation]
	@Id INT OUTPUT,
	@Name VARCHAR(250),
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Designation]
		(
			[Name],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@Name,
			@Remarks,
			@Status
		);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Designation]
		SET
			[Name] = @Name,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE
			[Id] = @Id;
	END

	SELECT @Id AS Id;
END
