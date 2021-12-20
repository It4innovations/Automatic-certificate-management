using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Functions
{
    public static class Email
    {
        public static bool SendEmail(string[] users, string[] attachements, string from, string subject, string body, bool toyourself = true)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.vsb.cz");
                mail.From = new MailAddress(from);
                foreach (string item in users)
                {
                    mail.To.Add(item);
                }
                if (toyourself)
                    mail.Bcc.Add(from);

                mail.Subject = subject;
                mail.Body = body;


                System.Net.Mail.Attachment attachment;

                if (attachements != null)
                {
                    foreach (string item in attachements)
                    {
                        attachment = new System.Net.Mail.Attachment(item);
                        mail.Attachments.Add(attachment);
                    }
                }

                SmtpServer.Port = 25;
                //   SmtpServer.Credentials = new System.Net.NetworkCredential("username", "password");
                //   SmtpServer.EnableSsl = true;

                SmtpServer.Send(mail);
                return true;
            }
            catch (Exception ex)
            {
              
            }
            return false;
        }

    }
}
