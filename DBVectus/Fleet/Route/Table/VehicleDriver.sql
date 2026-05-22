CREATE TABLE [dbo].[VehicleDriver]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
	[VehicleId] INT NOT NULL, 
	[DriverId] INT NOT NULL,
	[StartDateTime] DATETIME NOT NULL,
	[EndDateTime] DATETIME NULL,
	[Remarks] VARCHAR(MAX) NULL,
    CONSTRAINT [FK_VehicleDriver_ToDriver] FOREIGN KEY ([DriverId]) REFERENCES [Driver]([Id]), 
    CONSTRAINT [FK_VehicleDriver_ToVehicle] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicle]([Id]),
)
