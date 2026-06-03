CREATE TABLE [dbo].[StatutoryRule]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY,
    [Code] VARCHAR(20) NOT NULL UNIQUE,
    [Name] VARCHAR(250) NOT NULL,
    [ContributionAccount] VARCHAR(20) NULL,
    [RoundingMode] VARCHAR(20) NOT NULL DEFAULT 'None',
    [LedgerId] INT NULL,
    [StateUTId] INT NULL,
    [Remarks] VARCHAR(MAX) NULL,
    [Status] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [FK_StatutoryRule_ToLedger] FOREIGN KEY ([LedgerId]) REFERENCES [Ledger]([Id]),
    CONSTRAINT [FK_StatutoryRule_ToStateUT] FOREIGN KEY ([StateUTId]) REFERENCES [StateUT]([Id])
)
