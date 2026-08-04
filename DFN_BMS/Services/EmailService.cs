using DFN_BMS.DB;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;

public interface IEmailService
{
    Task SendEmailAsync(string subject, string body);
}

public class EmailService : IEmailService
{
    private readonly AppDbContext _context;

    public EmailService(AppDbContext context)
    {
        _context = context;
    }

    public async Task SendEmailAsync(string subject, string body)
    {
        var mailSetting = await _context.MAIL_SETTINGS
            .FirstOrDefaultAsync(x => x.Is_Active);

        if (mailSetting == null)
            return;

        using var smtp = new SmtpClient(mailSetting.Host, mailSetting.Port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(
                mailSetting.From_Mail,
                mailSetting.Password_Hash)
        };

        MailMessage mail = new MailMessage();

        mail.From = new MailAddress(mailSetting.From_Mail);

        foreach (var to in mailSetting.To_Mail.Split(','))
        {
            if (!string.IsNullOrWhiteSpace(to))
                mail.To.Add(to.Trim());
        }

        foreach (var cc in (mailSetting.CC_Mail ?? "").Split(','))
        {
            if (!string.IsNullOrWhiteSpace(cc))
                mail.CC.Add(cc.Trim());
        }

        mail.Subject = subject;
        mail.Body = body;
        mail.IsBodyHtml = true;

        await smtp.SendMailAsync(mail);
    }
}