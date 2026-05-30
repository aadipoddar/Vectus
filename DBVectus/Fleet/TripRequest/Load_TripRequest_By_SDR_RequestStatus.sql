CREATE PROCEDURE [dbo].[Load_TripRequest_By_SDR_RequestStatus]
	@SDRId INT = NULL,
	@RequestStatus VARCHAR(100) = NULL
AS
BEGIN
	SET NOCOUNT ON;

	SELECT *
	FROM [dbo].[TripRequest_Overview]
	WHERE [Status] = 1
		AND (@SDRId IS NULL OR [SDRId] = @SDRId)
		AND (@RequestStatus IS NULL OR [RequestStatus] = @RequestStatus)
	ORDER BY [TransactionDateTime] DESC;
END
