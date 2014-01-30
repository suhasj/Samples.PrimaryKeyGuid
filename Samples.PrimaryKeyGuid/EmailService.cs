using Microsoft.AspNet.Identity;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace IdentitySample.Models
{
    public class EmailService : IIdentityMessageService
    {
        public Task SendAsync(IdentityMessage message)
        {
            MailMessage email = new MailMessage("XXX@gmail.com", message.Destination);
            email.Subject = message.Subject;
            email.Body = message.Body;
            email.IsBodyHtml = true;

            var mailClient = new SmtpClient("smtp.gmail.com", 587) { Credentials = new NetworkCredential("XXX@gmail.com", "password"), EnableSsl = true };

            return mailClient.SendMailAsync(email);
        }

    }
}
