CREATE VIEW [dbo].[VehicleDocument_Renewal_Overview]
AS
SELECT
    [t].[Id],
    [t].[TransactionNo],
    [t].[TransactionDateTime],

	[t].[VehicleDocumentTypeId],
	[vdt].[Name] AS VehicleDocumentTypeName,
	[vdt].[Code] AS VehicleDocumentTypeCode,
	[t].[VehicleId],
	[v].[Code] AS VehicleCode,

	[t].[CurrentKM],
	[t].[Rate],
	[t].[RenewalDate],
	DATEDIFF(day, CAST(GETDATE() AS DATE), CAST([t].[RenewalDate] AS DATE)) AS DaysRemaining,

	[t].[DocumentUrl],

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
    [dbo].[VehicleDocument] t
INNER JOIN
	[dbo].[VehicleDocumentType] vdt ON t.VehicleDocumentTypeId = vdt.Id
INNER JOIN
	[dbo].[Vehicle] v ON t.VehicleId = v.Id
INNER JOIN
	[dbo].[User] AS u ON t.CreatedBy = u.Id
LEFT JOIN
	[dbo].[User] AS lm ON t.LastModifiedBy = lm.Id
WHERE
	t.Status = 1
	AND t.Id =
	(
		SELECT TOP 1 d.Id
		FROM [dbo].[VehicleDocument] d
		WHERE d.VehicleId = t.VehicleId
		  AND d.VehicleDocumentTypeId = t.VehicleDocumentTypeId
		  AND d.Status = 1
		ORDER BY d.TransactionDateTime DESC, d.Id DESC
	)
