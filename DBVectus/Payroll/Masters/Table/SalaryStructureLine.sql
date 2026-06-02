CREATE TABLE [dbo].[SalaryStructureLine]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY,
    [MasterId] INT NOT NULL,
    [SalaryComponentId] INT NOT NULL,
    [Amount] DECIMAL(18, 2) NULL,
    [OverridePercentage] DECIMAL(9, 4) NULL,
    [Status] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [FK_SalaryStructureLine_ToSalaryStructure] FOREIGN KEY ([MasterId]) REFERENCES [SalaryStructure]([Id]),
    CONSTRAINT [FK_SalaryStructureLine_ToSalaryComponent] FOREIGN KEY ([SalaryComponentId]) REFERENCES [SalaryComponent]([Id])
)
