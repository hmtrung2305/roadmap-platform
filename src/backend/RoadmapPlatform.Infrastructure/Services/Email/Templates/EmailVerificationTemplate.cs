namespace RoadmapPlatform.Infrastructure.Services.Email.Templates;

public static class EmailVerificationTemplate
{
    public static string Build(string code, int expirationMinutes)
    {
        return $"""
        <!DOCTYPE html>
        <html>
        <body style="margin:0; padding:0; background-color:#f6f7f9; font-family:Arial, Helvetica, sans-serif;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#f6f7f9; padding:32px 0;">
                <tr>
                    <td align="center">
                        <table width="100%" cellpadding="0" cellspacing="0" style="max-width:520px; background-color:#ffffff; border-radius:6px; overflow:hidden; border:1px solid #e5e7eb;">
                            <tr>
                                <td style="padding:28px 32px 16px 32px;">
                                    <div style="display:inline-block; padding:6px 10px; border-radius:999px; background-color:#ecfdf5; color:#047857; font-size:12px; font-weight:700;">
                                        ROADMAP PLATFORM
                                    </div>

                                    <h1 style="margin:18px 0 0 0; color:#111827; font-size:24px; font-weight:700;">
                                        Verify your email
                                    </h1>

                                    <p style="margin:14px 0 0 0; color:#4b5563; font-size:15px; line-height:1.6;">
                                        Use the verification code below to finish setting up your account.
                                    </p>
                                </td>
                            </tr>

                            <tr>
                                <td style="padding:8px 32px 24px 32px;">
                                    <div style="background-color:#f0fdf4; border:1px solid #bbf7d0; border-radius:8px; padding:20px; text-align:center;">
                                        <div style="color:#166534; font-size:12px; font-weight:700; text-transform:uppercase; letter-spacing:0.10em;">
                                            Verification code
                                        </div>

                                        <div style="margin-top:12px; color:#111827; font-size:34px; font-weight:800; letter-spacing:0.20em;">
                                            {code}
                                        </div>
                                    </div>
                                </td>
                            </tr>

                            <tr>
                                <td style="padding:0 32px 30px 32px;">
                                    <p style="margin:0; color:#6b7280; font-size:14px; line-height:1.6;">
                                        This code expires in {expirationMinutes} minutes. If you did not request this email, you can safely ignore it.
                                    </p>
                                </td>
                            </tr>

                            <tr>
                                <td style="padding:18px 32px; background-color:#f9fafb; border-top:1px solid #e5e7eb;">
                                    <p style="margin:0; color:#9ca3af; font-size:12px;">
                                        Sent by Roadmap Platform
                                    </p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
    }
}