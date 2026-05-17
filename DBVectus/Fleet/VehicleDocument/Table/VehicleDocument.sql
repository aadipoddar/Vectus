CREATE TABLE [dbo].[VehicleDocument]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [TransactionNo] VARCHAR(100) NOT NULL UNIQUE,
    [TransactionDateTime] DATETIME NOT NULL,
	[VehicleDocumentTypeId] INT NOT NULL,
    [VehicleId] INT NOT NULL,
    [CurrentKM] MONEY NOT NULL,
    [Rate] MONEY NOT NULL,
    [RenewalDate] DATETIME NOT NULL,
    [Remarks] VARCHAR(MAX) NULL,
    [DocumentUrl] VARCHAR(MAX) NULL,
	[CreatedBy] INT NOT NULL,
	[CreatedAt] DATETIME NOT NULL DEFAULT (((getdate() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time')),
	[CreatedFromPlatform] VARCHAR(MAX) NOT NULL,
	[Status] BIT NOT NULL DEFAULT 1,
	[LastModifiedBy] INT NULL,
	[LastModifiedAt] DATETIME NULL, 
	[LastModifiedFromPlatform] VARCHAR(MAX) NULL, 
    CONSTRAINT [FK_VehicleDocument_ToVehicleDocumentType] FOREIGN KEY ([VehicleDocumentTypeId]) REFERENCES [VehicleDocumentType]([Id]), 
    CONSTRAINT [FK_VehicleDocument_ToVehicle] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicle]([Id]),
    CONSTRAINT [FK_VehicleDocument_CreatedBy_ToUser] FOREIGN KEY ([CreatedBy]) REFERENCES [User]([Id]),
	CONSTRAINT [FK_VehicleDocument_LastModifiedBy_ToUser] FOREIGN KEY ([LastModifiedBy]) REFERENCES [User]([Id])
)
