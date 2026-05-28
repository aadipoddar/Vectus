CREATE PROCEDURE [dbo].[Load_User_By_Phone_Email]
	@PhoneEmail VARCHAR(250)
AS
BEGIN
	SELECT * FROM [dbo].[User]
	WHERE [Email] = @PhoneEmail
		OR [Phone] = @PhoneEmail
END
