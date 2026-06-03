CREATE PROCEDURE [dbo].[Insert_StatutorySlab]
	@Id INT OUTPUT,
	@MasterId INT,
	@FromAmount DECIMAL(18, 2),
	@ToAmount DECIMAL(18, 2),
	@FixedAmount DECIMAL(18, 2),
	@Rate DECIMAL(9, 4),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[StatutorySlab]
		(
			[MasterId],
			[FromAmount],
			[ToAmount],
			[FixedAmount],
			[Rate],
			[Status]
		)
		VALUES
		(
			@MasterId,
			@FromAmount,
			@ToAmount,
			@FixedAmount,
			@Rate,
			@Status
		);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[StatutorySlab]
		SET
			[MasterId] = @MasterId,
			[FromAmount] = @FromAmount,
			[ToAmount] = @ToAmount,
			[FixedAmount] = @FixedAmount,
			[Rate] = @Rate,
			[Status] = @Status
		WHERE
			[Id] = @Id;
	END

	SELECT @Id AS Id;
END
