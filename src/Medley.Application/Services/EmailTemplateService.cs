namespace Medley.Application.Services;

/// <summary>
/// Service for generating email content from templates
/// </summary>
public static class EmailTemplateService
{
    /// <summary>
    /// Generates email confirmation HTML content
    /// </summary>
    public static string GetEmailConfirmationTemplate(string confirmationLink)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #007bff; color: white; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Welcome to Medley!</h1>
        </div>
        <div class=""content"">
            <p>Thank you for registering with Medley.</p>
            <p>Please confirm your email address by clicking the button below:</p>
            <p style=""text-align: center;"">
                <a href=""{confirmationLink}"" class=""button"">Confirm Email Address</a>
            </p>
            <p>Or copy and paste this link into your browser:</p>
            <p style=""word-break: break-all; color: #007bff;"">{confirmationLink}</p>
            <p>If you didn't create an account with Medley, please ignore this email.</p>
        </div>
        <div class=""footer"">
            <p>&copy; 2025 Medley. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Generates password reset HTML content
    /// </summary>
    public static string GetPasswordResetTemplate(string resetLink)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #dc3545; color: white; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 12px; margin: 16px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Password Reset Request</h1>
        </div>
        <div class=""content"">
            <p>We received a request to reset your password for your Medley account.</p>
            <p>Click the button below to reset your password:</p>
            <p style=""text-align: center;"">
                <a href=""{resetLink}"" class=""button"">Reset Password</a>
            </p>
            <p>Or copy and paste this link into your browser:</p>
            <p style=""word-break: break-all; color: #dc3545;"">{resetLink}</p>
            <div class=""warning"">
                <strong>Security Notice:</strong> This link will expire in 24 hours. If you didn't request a password reset, please ignore this email and your password will remain unchanged.
            </div>
        </div>
        <div class=""footer"">
            <p>&copy; 2025 Medley. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }
}
