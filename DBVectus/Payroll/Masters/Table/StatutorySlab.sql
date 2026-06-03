CREATE TABLE [dbo].[StatutorySlab]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY,
    [MasterId] INT NOT NULL,
    [FromAmount] DECIMAL(18, 2) NOT NULL,
    [ToAmount] DECIMAL(18, 2) NULL,
    [FixedAmount] DECIMAL(18, 2) NULL,
    [Rate] DECIMAL(9, 4) NULL,
    [Status] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [FK_StatutorySlab_ToStatutoryRate] FOREIGN KEY ([MasterId]) REFERENCES [StatutoryRate]([Id])
)
