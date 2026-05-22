CREATE PROCEDURE [dbo].[Delete_TyreMounting]
	@Id INT
AS
BEGIN
	DELETE FROM [dbo].[TyreMounting] WHERE [Id] = @Id;

	SELECT 1 AS Success;
END
