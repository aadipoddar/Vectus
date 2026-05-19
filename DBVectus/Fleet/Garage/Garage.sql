CREATE TABLE [dbo].[Garage]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] VARCHAR(250) NOT NULL UNIQUE,
    [Code] VARCHAR(10) NOT NULL UNIQUE, 
    [LocationId] INT NOT NULL,
    [Remarks] VARCHAR(MAX) NULL,
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_Garage_ToLocation] FOREIGN KEY ([LocationId]) REFERENCES [dbo].[Location]([Id])
)
