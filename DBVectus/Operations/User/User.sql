CREATE TABLE [dbo].[User]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] VARCHAR(MAX) NOT NULL, 
    [Phone] VARCHAR(10) NOT NULL UNIQUE, 
    [Email] VARCHAR(250) NULL UNIQUE, 
    [Password] VARCHAR(MAX) NOT NULL,
    [Accounts] BIT NOT NULL DEFAULT 0,
    [Fleet] BIT NOT NULL DEFAULT 0,
    [Reports] BIT NOT NULL DEFAULT 0,
    [Admin] BIT NOT NULL DEFAULT 0, 
    [Remarks] VARCHAR(MAX) NULL,
    [Status] BIT NOT NULL DEFAULT 1, 
    [FailedAttempts] INT NOT NULL DEFAULT 0,
    [CodeResends] INT NOT NULL DEFAULT 0,
    [LastCode] INT NULL, 
    [LastCodeDeviceId] VARCHAR(MAX) NULL,
    [LastCodeDateTime] DATETIME NULL DEFAULT (((getdate() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time'))
)
