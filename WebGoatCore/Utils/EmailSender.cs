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
            SmtpClient client = new SmtpClient("smtp.office365.com")
            {
                Port = 587,
                EnableSsl = true,
                UseDefaultCredentials = true,
                Credentials = new System.Net.NetworkCredential("enter outlook email", "enter password", "outlook.com"),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            MailMessage mailMessage = new MailMessage();
            mailMessage.To.Add(new MailAddress(userEmail));
            mailMessage.From = new MailAddress("enter outlook email");
            mailMessage.Subject = "Confirm your email";
            mailMessage.Body = confirmationLink;
            mailMessage.IsBodyHtml = true;

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
