using System;
using System.Net;
using System.Net.Mail;
using System.Collections.Generic;
using RhdServices;

namespace ApiFetcher.Notification
{
    public class EmailNotifier
    {
        private static List<DateTime> emailSentTimestamps = new List<DateTime>(); // List to track sent timestamps
        private static readonly int emailLimit = 10; // Max number of emails allowed in the timeframe
        private static readonly TimeSpan timeLimit = TimeSpan.FromMinutes(30); // Timeframe limit

        public void SendEmailAlert(string subject, string body)
        {
            try
            {
                // Load SMTP settings
                NotificationConfigSettings setting = new NotificationConfigSettings();
                string senderEmail = setting.SmtpSettings("SenderEmail");
                string appPassword = setting.SmtpSettings("SenderPassword");
                string recipientEmail = setting.SmtpSettings("RecipientEmail");
                string host = setting.SmtpSettings("Host");
                string portString = setting.SmtpSettings("Port");

                if (!int.TryParse(portString, out int port))
                {
                    Console.WriteLine("Invalid port number in SMTP settings.");
                    return;
                }

                // Check if sending limit is exceeded
                CleanUpOldTimestamps();
                if (emailSentTimestamps.Count >= emailLimit)
                {
                    Console.WriteLine($"Email sending limit of {emailLimit} reached. Try again after some time.");
                    return;
                }

                // Configure SMTP client
                var smtpClient = new SmtpClient(host)
                {
                    Port = port,
                    Credentials = new NetworkCredential(senderEmail, appPassword),
                    EnableSsl = true
                };

                // Create email message
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false // Change to true if sending HTML content
                };
                mailMessage.To.Add(recipientEmail);

                // Send the email
                smtpClient.Send(mailMessage);

                // Record the timestamp of the email sent
                emailSentTimestamps.Add(DateTime.Now);
                Console.WriteLine("Email alert sent successfully.");
            }
            catch (SmtpException smtpEx)
            {
                Console.WriteLine($"SMTP error occurred: {smtpEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email alert: {ex.Message}");
            }
        }

        private void CleanUpOldTimestamps()
        {
            // Remove timestamps older than the allowed time limit
            emailSentTimestamps.RemoveAll(timestamp => DateTime.Now - timestamp > timeLimit);
        }
    }
}
