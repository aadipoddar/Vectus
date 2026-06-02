CREATE TABLE [dbo].[SalaryComponent]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY,
    [Code] VARCHAR(20) NOT NULL UNIQUE,
    [Name] VARCHAR(250) NOT NULL,
    [ComponentType] VARCHAR(10) NOT NULL DEFAULT 'Earning',
    [CalculationType] VARCHAR(20) NOT NULL DEFAULT 'Fixed',
    [BaseComponentCode] VARCHAR(20) NULL,
    [Percentage] DECIMAL(9, 4) NULL,
    [StatutoryRuleCode] VARCHAR(20) NULL,
    [IsTaxable] BIT NOT NULL DEFAULT 0,
    [IsPerquisite] BIT NOT NULL DEFAULT 0,
    [AffectsPFBase] BIT NOT NULL DEFAULT 0,
    [AffectsESIBase] BIT NOT NULL DEFAULT 0,
    [DisplayOrder] INT NOT NULL DEFAULT 0,
    [Remarks] VARCHAR(MAX) NULL,
    [Status] BIT NOT NULL DEFAULT 1,
)
