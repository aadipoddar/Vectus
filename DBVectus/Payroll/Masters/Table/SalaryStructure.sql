CREATE TABLE [dbo].[SalaryStructure]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY,
    [EmployeeId] INT NOT NULL,
    [EffectiveFrom] DATE NOT NULL,
    [CTC] DECIMAL(18, 2) NULL,
    [Remarks] VARCHAR(MAX) NULL,
    [Status] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [FK_SalaryStructure_ToEmployee] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee]([Id])
)
