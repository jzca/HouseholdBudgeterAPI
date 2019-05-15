using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;

namespace HouseholdBudgeterAPI.Models
{
    public class EmailService
    {
        private string SmtpHost = ConfigurationManager.
            AppSettings["SmtpHost"];
        private int SmtpPort = Convert.ToInt32(ConfigurationManager.
            AppSettings["SmtpPort"]);
        private string SmtpUsername = ConfigurationManager.
            AppSettings["SmtpUsername"];
        private string SmtpPassword = ConfigurationManager.
            AppSettings["SmtpPassword"];
        private string SmtpFrom = ConfigurationManager.
            AppSettings["SmtpFrom"];

        public void Send(string to, string subject, string body)
        {
            var message = new MailMessage(SmtpFrom, to);
            message.Body = body;
            message.Subject = subject;
            message.IsBodyHtml = true;

            var smtpClient = new SmtpClient(SmtpHost, SmtpPort);
            smtpClient.Credentials = new NetworkCredential(SmtpUsername, SmtpPassword);
            smtpClient.EnableSsl = true;
            smtpClient.Send(message);

        }
    }
}