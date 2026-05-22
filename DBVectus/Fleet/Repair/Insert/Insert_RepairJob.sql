CREATE PROCEDURE [dbo].[Insert_RepairJob]
	@Id INT OUTPUT,
	@MasterId INT,
	@Job VARCHAR(MAX),
	@Quantity MONEY,
	@Rate MONEY,
	@Total MONEY,
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[RepairJob]
		(
			[MasterId],
			[Job],
			[Quantity],
			[Rate],
			[Total],
			[Remarks],
			[Status]
		) VALUES
		(
			@MasterId,
			@Job,
			@Quantity,
			@Rate,
			@Total,
			@Remarks,
			@Status
		)

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[RepairJob]
		SET
			[MasterId] = @MasterId,
			[Job] = @Job,
			[Quantity] = @Quantity,
			[Rate] = @Rate,
			[Total] = @Total,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE
			[Id] = @Id
	END

	SELECT @Id AS Id
END
