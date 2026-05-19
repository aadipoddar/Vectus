CREATE VIEW [dbo].[TripRequest_Overview]
AS
SELECT
    [t].[Id],
    [t].[TransactionNo],
    [t].[CompanyId],
    [c].[Name] AS CompanyName,

    [t].[TransactionDateTime],
    [t].[FinancialYearId],
    CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,

    [t].[RouteId],
    [r].[Code] AS RouteCode,
    [frl].[Name] AS FromLocation,
    [torl].[Name] AS ToLocation,
    [frl].[Name] + ' - ' + [torl].[Name] AS RouteDisplay,
    [r].[EstimatedDistance],
    [r].[EstimatedHours],
    [r].[EstimatedFuelConsumption],
    [r].[EstimatedCost],

    [t].[VehicleId],
    [v].[Code] AS VehicleCode,
    [v].[SDRId],
    [sdr].[Name] AS SDRName,

    [t].[RequestStatus],

    [t].[Remarks],
    [t].[CreatedBy],
    [u].[Name] AS CreatedByName,
    [t].[CreatedAt],
    [t].[CreatedFromPlatform],
    [t].[LastModifiedBy],
    [lm].[Name] AS LastModifiedByUserName,
    [t].[LastModifiedAt],
    [t].[LastModifiedFromPlatform],

    [t].[Status]

FROM
    [dbo].[TripRequest] t
INNER JOIN
    [dbo].[Company] c ON t.CompanyId = c.Id
INNER JOIN
    [dbo].[FinancialYear] fy ON t.FinancialYearId = fy.Id
INNER JOIN
    [dbo].[Route] r ON t.RouteId = r.Id
INNER JOIN
    [dbo].[Location] frl ON r.FromLocationId = frl.Id
INNER JOIN
    [dbo].[Location] torl ON r.ToLocationId = torl.Id
INNER JOIN
    [dbo].[Vehicle] v ON t.VehicleId = v.Id
LEFT JOIN
    [dbo].[SDR] sdr ON v.SDRId = sdr.Id
INNER JOIN
    [dbo].[User] AS u ON t.CreatedBy = u.Id
LEFT JOIN
    [dbo].[User] AS lm ON t.LastModifiedBy = lm.Id
