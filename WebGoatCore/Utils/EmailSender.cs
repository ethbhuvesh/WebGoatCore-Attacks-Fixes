using System;
using System.Net.Mail;

namespace WebGoatCore
{
    public class EmailSender
    {
        public static void Send(string to, string subject, string messageBody)
        {
            var message = new MailMessage("WebAppSecurityClass@gmail.com", to)
            {
                Subject = subject,
                IsBodyHtml = true,
                Body = messageBody,
            };
            var client = new SmtpClient() { EnableSsl = true };
            try
            {
                client.Send(message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static void Send(MailMessage message)
        {
            var client = new SmtpClient() { EnableSsl = true };
            try
            {
                client.Send(message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool SendEmail(string userEmail, string confirmationLink)
        {
            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress("pitifulbrad9876@gmail.com");
            mailMessage.To.Add(new MailAddress(userEmail));

            mailMessage.Subject = "Confirm your email";
            mailMessage.IsBodyHtml = true;
            mailMessage.Body = confirmationLink;

            SmtpClient client = new SmtpClient();
            client.Credentials = new System.Net.NetworkCredential("pitifulbrad9876@gmail.com", "ABcd1234$$");
            client.Host = "smtp.gmail.com";
            client.Port = 465;
            client.EnableSsl = true;

            try
            {
                client.Send(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;

        }

    }
}
