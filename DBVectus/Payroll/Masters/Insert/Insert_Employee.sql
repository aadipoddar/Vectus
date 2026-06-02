CREATE PROCEDURE [dbo].[Insert_Employee]
	@Id INT OUTPUT,
	@Code VARCHAR(10),
	@Name VARCHAR(250),
	@DepartmentId INT,
	@DesignationId INT,
	@EmployeeLocationId INT,
	@DateOfJoining DATE,
	@DateOfLeaving DATE,
	@PaymentMode VARCHAR(10),
	@BankName VARCHAR(250),
	@BankAccountNo VARCHAR(50),
	@IFSC VARCHAR(11),
	@PANNo VARCHAR(10),
	@AadhaarNo VARCHAR(12),
	@PFUAN VARCHAR(12),
	@PFNumber VARCHAR(50),
	@ESINumber VARCHAR(17),
	@IsPFApplicable BIT,
	@IsESIApplicable BIT,
	@IsPTApplicable BIT,
	@IsLWFApplicable BIT,
	@ContributeOnHigherPFWage BIT,
	@ESICoveredUpto DATE,
	@Phone VARCHAR(10),
	@Email VARCHAR(250),
	@Address VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Employee]
		(
			[Code],
			[Name],
			[DepartmentId],
			[DesignationId],
			[EmployeeLocationId],
			[DateOfJoining],
			[DateOfLeaving],
			[PaymentMode],
			[BankName],
			[BankAccountNo],
			[IFSC],
			[PANNo],
			[AadhaarNo],
			[PFUAN],
			[PFNumber],
			[ESINumber],
			[IsPFApplicable],
			[IsESIApplicable],
			[IsPTApplicable],
			[IsLWFApplicable],
			[ContributeOnHigherPFWage],
			[ESICoveredUpto],
			[Phone],
			[Email],
			[Address],
			[Status]
		)
		VALUES
		(
			@Code,
			@Name,
			@DepartmentId,
			@DesignationId,
			@EmployeeLocationId,
			@DateOfJoining,
			@DateOfLeaving,
			@PaymentMode,
			@BankName,
			@BankAccountNo,
			@IFSC,
			@PANNo,
			@AadhaarNo,
			@PFUAN,
			@PFNumber,
			@ESINumber,
			@IsPFApplicable,
			@IsESIApplicable,
			@IsPTApplicable,
			@IsLWFApplicable,
			@ContributeOnHigherPFWage,
			@ESICoveredUpto,
			@Phone,
			@Email,
			@Address,
			@Status
		);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Employee]
		SET
			[Code] = @Code,
			[Name] = @Name,
			[DepartmentId] = @DepartmentId,
			[DesignationId] = @DesignationId,
			[EmployeeLocationId] = @EmployeeLocationId,
			[DateOfJoining] = @DateOfJoining,
			[DateOfLeaving] = @DateOfLeaving,
			[PaymentMode] = @PaymentMode,
			[BankName] = @BankName,
			[BankAccountNo] = @BankAccountNo,
			[IFSC] = @IFSC,
			[PANNo] = @PANNo,
			[AadhaarNo] = @AadhaarNo,
			[PFUAN] = @PFUAN,
			[PFNumber] = @PFNumber,
			[ESINumber] = @ESINumber,
			[IsPFApplicable] = @IsPFApplicable,
			[IsESIApplicable] = @IsESIApplicable,
			[IsPTApplicable] = @IsPTApplicable,
			[IsLWFApplicable] = @IsLWFApplicable,
			[ContributeOnHigherPFWage] = @ContributeOnHigherPFWage,
			[ESICoveredUpto] = @ESICoveredUpto,
			[Phone] = @Phone,
			[Email] = @Email,
			[Address] = @Address,
			[Status] = @Status
		WHERE
			[Id] = @Id;
	END

	SELECT @Id AS Id;
END
