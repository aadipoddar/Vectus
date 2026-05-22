CREATE TABLE [dbo].[TyreMounting]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
	[TyreNo] VARCHAR(MAX) NOT NULL,
	[TyreCompanyId] INT NOT NULL,
	[TyreModel] VARCHAR(MAX) NULL,
	[VehicleId] INT NOT NULL, 
	[MountingKM] MONEY NOT NULL,
	[DismountingKM] MONEY NULL,
	[MountingDateTime] DATETIME NOT NULL,
	[DismountingDateTime] DATETIME NULL,
	[Remarks] VARCHAR(MAX) NULL,
	CONSTRAINT [FK_TyreMounting_ToVehicle] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicle]([Id]),
	CONSTRAINT [FK_TyreMounting_ToTyreCompany] FOREIGN KEY ([TyreCompanyId]) REFERENCES [TyreCompany]([Id])
)
