CREATE PROCEDURE [dbo].[Insert_VehicleDocumentType]
	@Id INT OUTPUT,
	@Name VARCHAR(250),
	@Code VARCHAR(10),
	@Rate MONEY,
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[VehicleDocumentType]
		(
			[Name],
			[Code],
			[Rate],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@Name,
			@Code,
			@Rate,
			@Remarks,
			@Status
		);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[VehicleDocumentType]
		SET
			[Name] = @Name,
			[Code] = @Code,
			[Rate] = @Rate,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE
			[Id] = @Id;
	END

	SELECT @Id AS Id;
END