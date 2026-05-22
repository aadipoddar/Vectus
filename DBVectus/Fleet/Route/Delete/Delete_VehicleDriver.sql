CREATE PROCEDURE [dbo].[Delete_VehicleDriver]
	@Id INT
AS
BEGIN
	DELETE FROM [dbo].[VehicleDriver] WHERE Id = @Id

	SELECT 1 AS Success
END