using System;
using System.Net;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
namespace ADLRestaurant.Helpers
{
    public class EmailHelper
    {
        private readonly IConfiguration _config;

        public EmailHelper(IConfiguration config)
        {
            _config = config;
        }

        public void sendmailtest()
        {

            var email = new MimeMessage();

            email.From.Add(new MailboxAddress("Sender Name", "info@adlsoftcodes.com"));
            email.To.Add(new MailboxAddress("Receiver Name", "satyam0297@gmail.com"));

            email.Subject = "Testing out email sending";
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = "<b>Hello all the way from the land of C#</b>"
            };

            using (var smtp = new SmtpClient())
            {
                smtp.Connect("adlsoftcodes.com", 587, false);

                // Note: only needed if the SMTP server requires authentication
                smtp.Authenticate("info@adlsoftcodes.com", "S@tyam@897");

                smtp.Send(email);
                smtp.Disconnect(true);
            }
        }
        public string SendWelcomeEmail(string toEmail, string restaurantId,string userid, string pin)
        {
            try
            {
                var fromEmail = _config["EmailSettings:FromEmail"];
                var fromPassword = _config["EmailSettings:Password"];
                var host = _config["EmailSettings:Host"];
                var port = int.Parse(_config["EmailSettings:Port"]);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("ADLSoftTCodes", fromEmail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = "Welcome to ADLSoftTCodes - Restaurant Registered";

                message.Body = new TextPart("html")
                {
                    Text = $@"
                    <h3>Welcome to ADLSoftTCodes!</h3>
                    <p>Your restaurant has been successfully registered.</p>
                    <p><strong>Restaurant ID:</strong> {restaurantId}</p>
                      <p><strong>User ID:</strong> {userid}</p>
                    <p><strong>Login Pin:</strong> {pin}</p>
                    <p>Please log in and change your password immediately.</p>
                    <br/>
                    <p>Regards,<br/>ADLSoftTCodes Team</p>"
                };

                using var client = new SmtpClient();
                client.Connect(host, port, MailKit.Security.SecureSocketOptions.StartTls);
                client.Authenticate(fromEmail, fromPassword);
                client.Send(message);
                client.Disconnect(true);

                return "Email sent successfully.";
            }
            catch (Exception ex)
            {
                return "Error sending email: " + ex.Message;
            }
        }
    }
}
