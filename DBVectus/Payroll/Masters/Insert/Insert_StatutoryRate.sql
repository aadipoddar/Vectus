CREATE PROCEDURE [dbo].[Insert_StatutoryRate]
	@Id INT OUTPUT,
	@MasterId INT,
	@EffectiveFrom DATE,
	@EmployeeRate DECIMAL(9, 4),
	@EmployerRate DECIMAL(9, 4),
	@WageCeiling DECIMAL(18, 2),
	@MaxAmount DECIMAL(18, 2),
	@MinAmount DECIMAL(18, 2),
	@MinBasePercentOfGross DECIMAL(9, 4),
	@StandardDeduction DECIMAL(18, 2),
	@RebateAmount DECIMAL(18, 2),
	@RebateIncomeLimit DECIMAL(18, 2),
	@CessPercent DECIMAL(9, 4),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[StatutoryRate]
		(
			[MasterId],
			[EffectiveFrom],
			[EmployeeRate],
			[EmployerRate],
			[WageCeiling],
			[MaxAmount],
			[MinAmount],
			[MinBasePercentOfGross],
			[StandardDeduction],
			[RebateAmount],
			[RebateIncomeLimit],
			[CessPercent],
			[Status]
		)
		VALUES
		(
			@MasterId,
			@EffectiveFrom,
			@EmployeeRate,
			@EmployerRate,
			@WageCeiling,
			@MaxAmount,
			@MinAmount,
			@MinBasePercentOfGross,
			@StandardDeduction,
			@RebateAmount,
			@RebateIncomeLimit,
			@CessPercent,
			@Status
		);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[StatutoryRate]
		SET
			[MasterId] = @MasterId,
			[EffectiveFrom] = @EffectiveFrom,
			[EmployeeRate] = @EmployeeRate,
			[EmployerRate] = @EmployerRate,
			[WageCeiling] = @WageCeiling,
			[MaxAmount] = @MaxAmount,
			[MinAmount] = @MinAmount,
			[MinBasePercentOfGross] = @MinBasePercentOfGross,
			[StandardDeduction] = @StandardDeduction,
			[RebateAmount] = @RebateAmount,
			[RebateIncomeLimit] = @RebateIncomeLimit,
			[CessPercent] = @CessPercent,
			[Status] = @Status
		WHERE
			[Id] = @Id;
	END

	SELECT @Id AS Id;
END
