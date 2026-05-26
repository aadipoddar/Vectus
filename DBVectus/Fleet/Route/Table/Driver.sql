CREATE TABLE [dbo].[Driver]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] VARCHAR(MAX) NOT NULL,
    [Mobile] VARCHAR(10) NOT NULL UNIQUE,
    [Code] VARCHAR(10) NOT NULL UNIQUE, 
    [LicenseUrl] VARCHAR(MAX) NULL,
    [LicenseNo] VARCHAR(MAX) NULL,
    [LicenseExpiryDateTime] DATETIME NULL,
    [Remarks] VARCHAR(MAX) NULL,
    [Status] BIT NOT NULL DEFAULT 1
)
