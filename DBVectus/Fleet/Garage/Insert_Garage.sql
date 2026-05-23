CREATE PROCEDURE [dbo].[Insert_Garage]
	@Id INT OUTPUT,
	@Name VARCHAR(250),
	@Code VARCHAR(10),
	@LocationId INT,
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Garage]
		(
			[Name],
			[Code],
			[LocationId],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@Name,
			@Code,
			@LocationId,
			@Remarks,
			@Status
		);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Garage]
		SET
			[Name] = @Name,
			[Code] = @Code,
			[LocationId] = @LocationId,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END