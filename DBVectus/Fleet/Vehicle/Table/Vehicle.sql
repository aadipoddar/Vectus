CREATE TABLE [dbo].[Vehicle]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Code] VARCHAR(250) NOT NULL UNIQUE, 
    [ShortCode] VARCHAR(250) NOT NULL, 
    [ChasisCode] VARCHAR(250) NULL UNIQUE, 
    [EngineCode] VARCHAR(250) NULL UNIQUE, 
    [PurchaseDate] DATETIME NOT NULL, 
    [OpeningKM] MONEY NOT NULL, 
    [VehicleTypeId] INT NOT NULL, 
    [CompanyId] INT NOT NULL,
    [HDRId] INT NULL,
    [Remarks] VARCHAR(MAX) NULL, 
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_Vehicle_ToVehicleType] FOREIGN KEY ([VehicleTypeId]) REFERENCES [VehicleType]([Id]), 
    CONSTRAINT [FK_Vehicle_ToCompany] FOREIGN KEY ([CompanyId]) REFERENCES [Company]([Id]),
    CONSTRAINT [FK_Vehicle_ToHDR] FOREIGN KEY ([HDRId]) REFERENCES [HDR]([Id])
)
