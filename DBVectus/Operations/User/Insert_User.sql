CREATE PROCEDURE [dbo].[Insert_User]
	@Id INT OUTPUT,
	@Name VARCHAR(MAX),
	@Phone VARCHAR(10),
	@Email VARCHAR(MAX),
	@Password VARCHAR(MAX),
	@Accounts BIT,
	@Fleet BIT,
	@Reports BIT,
	@Admin BIT,
	@Remarks VARCHAR(MAX),
	@Status BIT,
	@FailedAttempts INT,
	@CodeResends INT,
	@LastCode INT,
	@LastCodeDeviceId VARCHAR(MAX),
	@LastCodeDateTime DATETIME
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[User]
		(
			[Name],
			[Phone],
			[Email],
			[Password],
			[Accounts],
			[Fleet],
			[Reports],
			[Admin],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@Name,
			@Phone,
			@Email,
			@Password,
			@Accounts,
			@Fleet,
			@Reports,
			@Admin,
			@Remarks,
			@Status
		);

		SET @Id = SCOPE_IDENTITY();
	END
	ELSE
	BEGIN
		UPDATE [dbo].[User]
		SET 
			[Name] = @Name,
			[Phone] = @Phone,
			[Email] = @Email,
			[Password] = @Password,
			[Accounts] = @Accounts,
			[Fleet] = @Fleet,
			[Reports] = @Reports,
			[Admin] = @Admin,
			[Remarks] = @Remarks,
			[Status] = @Status,
			[FailedAttempts] = @FailedAttempts,
			[CodeResends] = @CodeResends,
			[LastCode] = @LastCode,
			[LastCodeDateTime] = @LastCodeDateTime,
			[LastCodeDeviceId] = @LastCodeDeviceId
		WHERE
			[Id] = @Id
	END

	SELECT @Id AS Id
END