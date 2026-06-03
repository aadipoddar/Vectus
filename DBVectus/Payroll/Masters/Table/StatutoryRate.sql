CREATE TABLE [dbo].[StatutoryRate]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY,
    [MasterId] INT NOT NULL,
    [EffectiveFrom] DATE NOT NULL,
    [EmployeeRate] DECIMAL(9, 4) NULL,
    [EmployerRate] DECIMAL(9, 4) NULL,
    [WageCeiling] DECIMAL(18, 2) NULL,
    [MaxAmount] DECIMAL(18, 2) NULL,
    [MinAmount] DECIMAL(18, 2) NULL,
    [MinBasePercentOfGross] DECIMAL(9, 4) NULL,
    [StandardDeduction] DECIMAL(18, 2) NULL,
    [RebateAmount] DECIMAL(18, 2) NULL,
    [RebateIncomeLimit] DECIMAL(18, 2) NULL,
    [CessPercent] DECIMAL(9, 4) NULL,
    [Status] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [FK_StatutoryRate_ToStatutoryRule] FOREIGN KEY ([MasterId]) REFERENCES [StatutoryRule]([Id])
)
