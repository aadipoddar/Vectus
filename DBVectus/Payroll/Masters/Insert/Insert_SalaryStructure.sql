CREATE PROCEDURE [dbo].[Insert_SalaryStructure]
	@Id INT OUTPUT,
	@EmployeeId INT,
	@EffectiveFrom DATE,
	@CTC DECIMAL(18, 2),
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[SalaryStructure]
		(
			[EmployeeId],
			[EffectiveFrom],
			[CTC],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@EmployeeId,
			@EffectiveFrom,
			@CTC,
			@Remarks,
			@Status
		);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[SalaryStructure]
		SET
			[EmployeeId] = @EmployeeId,
			[EffectiveFrom] = @EffectiveFrom,
			[CTC] = @CTC,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE
			[Id] = @Id;
	END

	SELECT @Id AS Id;
END
