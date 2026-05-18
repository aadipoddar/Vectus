CREATE TABLE [dbo].[Route]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [FromLocationId] INT NOT NULL,
    [ToLocationId] INT NOT NULL,
    [Code] VARCHAR(10) NOT NULL UNIQUE, 
    [EstimatedHours] INT NOT NULL,
    [EstimatedDistance] INT NOT NULL,
    [EstimatedFuelConsumption] INT NOT NULL,
    [EstimatedCost] MONEY NOT NULL,
    [Remarks] VARCHAR(MAX) NULL,
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_Route_FromLocation] FOREIGN KEY ([FromLocationId]) REFERENCES [Location]([Id]),
    CONSTRAINT [FK_Route_ToLocation] FOREIGN KEY ([ToLocationId]) REFERENCES [Location]([Id])
)
