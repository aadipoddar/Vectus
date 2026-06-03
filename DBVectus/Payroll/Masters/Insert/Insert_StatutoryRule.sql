CREATE PROCEDURE [dbo].[Insert_StatutoryRule]
	@Id INT OUTPUT,
	@Code VARCHAR(20),
	@Name VARCHAR(250),
	@ContributionAccount VARCHAR(20),
	@RoundingMode VARCHAR(20),
	@LedgerId INT,
	@StateUTId INT,
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[StatutoryRule]
		(
			[Code],
			[Name],
			[ContributionAccount],
			[RoundingMode],
			[LedgerId],
			[StateUTId],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@Code,
			@Name,
			@ContributionAccount,
			@RoundingMode,
			@LedgerId,
			@StateUTId,
			@Remarks,
			@Status
		);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[StatutoryRule]
		SET
			[Code] = @Code,
			[Name] = @Name,
			[ContributionAccount] = @ContributionAccount,
			[RoundingMode] = @RoundingMode,
			[LedgerId] = @LedgerId,
			[StateUTId] = @StateUTId,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE
			[Id] = @Id;
	END

	SELECT @Id AS Id;
END
