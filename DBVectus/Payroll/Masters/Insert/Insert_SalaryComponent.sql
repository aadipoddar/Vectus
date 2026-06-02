CREATE PROCEDURE [dbo].[Insert_SalaryComponent]
	@Id INT OUTPUT,
	@Code VARCHAR(20),
	@Name VARCHAR(250),
	@ComponentType VARCHAR(10),
	@CalculationType VARCHAR(20),
	@BaseComponentCode VARCHAR(20),
	@Percentage DECIMAL(9, 4),
	@StatutoryRuleCode VARCHAR(20),
	@IsTaxable BIT,
	@IsPerquisite BIT,
	@AffectsPFBase BIT,
	@AffectsESIBase BIT,
	@DisplayOrder INT,
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[SalaryComponent]
		(
			[Code],
			[Name],
			[ComponentType],
			[CalculationType],
			[BaseComponentCode],
			[Percentage],
			[StatutoryRuleCode],
			[IsTaxable],
			[IsPerquisite],
			[AffectsPFBase],
			[AffectsESIBase],
			[DisplayOrder],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@Code,
			@Name,
			@ComponentType,
			@CalculationType,
			@BaseComponentCode,
			@Percentage,
			@StatutoryRuleCode,
			@IsTaxable,
			@IsPerquisite,
			@AffectsPFBase,
			@AffectsESIBase,
			@DisplayOrder,
			@Remarks,
			@Status
		);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[SalaryComponent]
		SET
			[Code] = @Code,
			[Name] = @Name,
			[ComponentType] = @ComponentType,
			[CalculationType] = @CalculationType,
			[BaseComponentCode] = @BaseComponentCode,
			[Percentage] = @Percentage,
			[StatutoryRuleCode] = @StatutoryRuleCode,
			[IsTaxable] = @IsTaxable,
			[IsPerquisite] = @IsPerquisite,
			[AffectsPFBase] = @AffectsPFBase,
			[AffectsESIBase] = @AffectsESIBase,
			[DisplayOrder] = @DisplayOrder,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE
			[Id] = @Id;
	END

	SELECT @Id AS Id;
END
