using MailKit.Net.Smtp;
using MimeKit;
using System.Net.Mail;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace AuthApi.Services
{
    public class EmailService
    {
        public static async Task SendEmail(string toEmail, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("AuthApp", "hasibislam2k18@gmail.com"));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, false);
            await client.AuthenticateAsync("hasibislam2k18@gmail.com", "gttsqwmzweopqyzp");
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
