CREATE PROCEDURE [dbo].[Insert_EmployeeLocation]
	@Id INT OUTPUT,
	@Name VARCHAR(250),
	@StateUTId INT,
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[EmployeeLocation]
		(
			[Name],
			[StateUTId],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@Name,
			@StateUTId,
			@Remarks,
			@Status
		);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[EmployeeLocation]
		SET
			[Name] = @Name,
			[StateUTId] = @StateUTId,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE
			[Id] = @Id;
	END

	SELECT @Id AS Id;
END
