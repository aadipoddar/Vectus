using MailKit.Net.Smtp;

using MimeKit;

using VectusLibrary.DataAccess;
using VectusLibrary.Operations.Models;

namespace VectusLibrary.Utils.MailUtils;

public static class AuthenticationMailing
{
	public static async Task SendLoginCodeEmail(UserModel user, string code, string redirectLink, int codeExpiryMinutes)
	{
		var subject = $"Your Login Code for {Secrets.DatabaseName}";
		var htmlBody = GenerateLoginCodeEmailHtml(user, code, codeExpiryMinutes, redirectLink);
		await SendEmailToUser(user.Name, user.Email, subject, htmlBody);
	}

	private static async Task SendEmailToUser(string toName, string toEmail, string subject, string htmlBody)
	{
		var message = new MimeMessage();
		message.From.Add(new MailboxAddress("AadiSoft", Secrets.Email));
		message.To.Add(new MailboxAddress(toName, toEmail));
		message.Subject = subject;

		var bodyBuilder = new BodyBuilder
		{
			HtmlBody = htmlBody
		};
		message.Body = bodyBuilder.ToMessageBody();

		using var client = new SmtpClient();
		await client.ConnectAsync("smtp.gmail.com", 465, true);
		await client.AuthenticateAsync(Secrets.Email, Secrets.EmailPassword);
		await client.SendAsync(message);
		await client.DisconnectAsync(true);
	}

	private static string GenerateLoginCodeEmailHtml(UserModel user, string code, int codeExpiryMinutes, string redirectLink) => $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Your Login Code</title>
    <style>
        @media only screen and (max-width: 600px) {{
            .email-container {{
                width: 100% !important;
            }}
            .outer-padding {{
                padding: 20px 10px !important;
            }}
            .content-padding {{
                padding: 30px 20px !important;
            }}
        }}
    </style>
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: {MailTheme.PageBackground};"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse; background-color: {MailTheme.PageBackground};"">
        <tr>
            <td align=""center"" class=""outer-padding"" style=""padding: 40px 20px;"">
                <table role=""presentation"" class=""email-container"" style=""width: 600px; max-width: 100%; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 20px rgba(59, 130, 246, 0.15);"">
                    
                    <!-- Header -->
                    <tr>
                        <td class=""header-padding"" style=""padding: 30px; text-align: center; background-color: transparent; border-radius: 12px 12px 0 0;"">
                            <img src=""{Secrets.OnlineFullLogoPath}"" alt=""{Secrets.DatabaseName}"" style=""max-width: 400px; width: 100%; height: auto; display: block; margin: 0 auto;"" />
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td class=""content-padding"" style=""padding: 40px;"">
                            <h2 style=""margin: 0 0 20px 0; color: #333333; font-size: 24px; font-weight: 600;"">Hello {user.Name},</h2>
                            <p style=""margin: 0 0 30px 0; color: #666666; font-size: 16px; line-height: 1.6;"">
                                You've requested a login code for your {Secrets.DatabaseName} account. Use the code below to complete your sign-in:
                            </p>
                            
                            <!-- Code Box -->
                            <table role=""presentation"" style=""width: 100%; border-collapse: collapse; margin: 30px 0;"">
                                <tr>
                                    <td align=""center"" style=""padding: 30px; background-color: {MailTheme.SurfaceTint}; border-radius: 8px; border: 2px dashed {MailTheme.SurfaceBorder};"">
                                        <div style=""font-size: 36px; font-weight: 700; color: #2563eb; letter-spacing: 8px; font-family: 'Courier New', monospace;"">{code}</div>
                                    </td>
                                </tr>
                            </table>
                            
                            <p style=""margin: 30px 0 20px 0; color: #666666; font-size: 16px; line-height: 1.6;"">
                                This code will expire in <strong>{codeExpiryMinutes} minutes</strong> for your security.
                            </p>
                            
                            <!-- CTA Button -->
                            <table role=""presentation"" style=""width: 100%; border-collapse: collapse; margin: 20px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{redirectLink}"" style=""display: inline-block; padding: 14px 40px; background: linear-gradient(135deg, #3b82f6 0%, #1e40af 100%); color: #ffffff; text-decoration: none; border-radius: 6px; font-weight: 600; font-size: 16px; box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3);"">Go to {Secrets.DatabaseName} Login</a>
                                    </td>
                                </tr>
                            </table>
                            
                            <!-- Warning Box -->
                            <table role=""presentation"" style=""width: 100%; border-collapse: collapse; margin: 30px 0;"">
                                <tr>
                                    <td style=""padding: 20px; background-color: #fff3cd; border-left: 4px solid #ffc107; border-radius: 4px;"">
                                        <p style=""margin: 0; color: #856404; font-size: 14px; line-height: 1.6;"">
                                            <strong>⚠️ Security Notice:</strong> If you didn't request this code, please ignore this email and ensure your account is secure.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td class=""footer-padding"" style=""padding: 30px 40px; background-color: #f8fafc; border-radius: 0 0 12px 12px; border-top: 1px solid #e2e8f0;"">
                            <p style=""margin: 0 0 10px 0; color: #999999; font-size: 14px; line-height: 1.6;"">
                                Best regards,<br>
                                <strong style=""color: #1e40af;"">The AadiSoft Team</strong>
                            </p>
                            <p style=""margin: 20px 0 0 0; color: #999999; font-size: 12px; line-height: 1.6;"">
                                This is an automated message, please do not reply to this email.<br>
                                <a href=""{Secrets.AadiSoftWebsite}"" style=""color: #2563eb; text-decoration: none;"">Visit our website</a>
                            </p>
                        </td>
                    </tr>
                </table>
                
                <!-- Footer Text -->
                <table role=""presentation"" class=""email-container"" style=""width: 600px; max-width: 100%; border-collapse: collapse; margin-top: 20px;"">
                    <tr>
                        <td align=""center"" style=""padding: 0 40px;"">
                            <p style=""margin: 0; color: #999999; font-size: 12px; line-height: 1.6;"">
                                © {DateTime.Now.Year} AadiSoft. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
}
