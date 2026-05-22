CREATE VIEW [dbo].[Repair_Overview]
AS
SELECT
    [r].[Id],
    [r].[TransactionNo],
    [r].[CompanyId],
    [c].[Name] AS CompanyName,

    [r].[TransactionDateTime],
    [r].[FinancialYearId],
	CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,

	[r].[GarageId],
	[g].[Name] AS GarageName,

	[r].[VehicleId],
	[v].[Code] AS VehicleCode,
	[r].[CurrentKM],

	[r].[GarageInDateTime],
	[r].[GarageOutDateTime],

	[r].[TotalItems],
	[r].[TotalQuantity],
	[r].[TotalAmount],

    [r].[Remarks],
	[r].[CreatedBy],
	[u].[Name] AS CreatedByName,
	[r].[CreatedAt],
	[r].[CreatedFromPlatform],
	[r].[LastModifiedBy],
	[lm].[Name] AS LastModifiedByUserName,
	[r].[LastModifiedAt],
	[r].[LastModifiedFromPlatform],

	[r].[Status]

FROM
    [dbo].[Repair] r
INNER JOIN
    [dbo].[Company] c ON r.CompanyId = c.Id
INNER JOIN
    [dbo].[FinancialYear] fy ON r.FinancialYearId = fy.Id
INNER JOIN
	[dbo].[Garage] g ON r.GarageId = g.Id
INNER JOIN
	[dbo].[Vehicle] v ON r.VehicleId = v.Id
INNER JOIN
	[dbo].[User] AS u ON r.CreatedBy = u.Id
LEFT JOIN
	[dbo].[User] AS lm ON r.LastModifiedBy = lm.Id
