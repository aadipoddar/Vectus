CREATE PROCEDURE [dbo].[Insert_TyreMounting]
	@Id INT OUTPUT,
	@TyreNo VARCHAR(MAX),
	@TyreCompanyId INT,
	@TyreModel VARCHAR(MAX),
	@VehicleId INT,
	@MountingKM MONEY,
	@DismountingKM MONEY,
	@MountingDateTime DATETIME,
	@DismountingDateTime DATETIME,
	@Remarks VARCHAR(MAX)
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[TyreMounting]
		(
			[TyreNo],
			[TyreCompanyId],
			[TyreModel],
			[VehicleId],
			[MountingKM],
			[DismountingKM],
			[MountingDateTime],
			[DismountingDateTime],
			[Remarks]
		)
		VALUES
		(
			@TyreNo,
			@TyreCompanyId,
			@TyreModel,
			@VehicleId,
			@MountingKM,
			@DismountingKM,
			@MountingDateTime,
			@DismountingDateTime,
			@Remarks
		);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[TyreMounting]
		SET
			[TyreNo] = @TyreNo,
			[TyreCompanyId] = @TyreCompanyId,
			[TyreModel] = @TyreModel,
			[VehicleId] = @VehicleId,
			[MountingKM] = @MountingKM,
			[DismountingKM] = @DismountingKM,
			[MountingDateTime] = @MountingDateTime,
			[DismountingDateTime] = @DismountingDateTime,
			[Remarks] = @Remarks
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END
