CREATE PROCEDURE [dbo].[Insert_SalaryStructureLine]
	@Id INT OUTPUT,
	@MasterId INT,
	@SalaryComponentId INT,
	@Amount DECIMAL(18, 2),
	@OverridePercentage DECIMAL(9, 4),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[SalaryStructureLine]
		(
			[MasterId],
			[SalaryComponentId],
			[Amount],
			[OverridePercentage],
			[Status]
		)
		VALUES
		(
			@MasterId,
			@SalaryComponentId,
			@Amount,
			@OverridePercentage,
			@Status
		);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[SalaryStructureLine]
		SET
			[MasterId] = @MasterId,
			[SalaryComponentId] = @SalaryComponentId,
			[Amount] = @Amount,
			[OverridePercentage] = @OverridePercentage,
			[Status] = @Status
		WHERE
			[Id] = @Id;
	END

	SELECT @Id AS Id;
END
