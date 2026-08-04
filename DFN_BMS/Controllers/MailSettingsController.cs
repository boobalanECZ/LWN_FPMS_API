using DFN_BMS.DB;
using DFN_BMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

namespace DFN_BMS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MailSettingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MailSettingsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.MAIL_SETTINGS
                .Where(x => x.Is_Active)
                .OrderByDescending(x => x.Mail_Setting_ID)
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _context.MAIL_SETTINGS
                .FirstOrDefaultAsync(x => x.Mail_Setting_ID == id);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Save(MailSettings model)
        {

            var existing = await _context.MAIL_SETTINGS
       .AnyAsync(x => x.Is_Active);

            if (existing)
            {
                return BadRequest("Mail Settings already exists. Please update or delete the existing record.");
            }
            if (string.IsNullOrWhiteSpace(model.Host))
                return BadRequest("Host is required");

            if (model.Port <= 0)
                return BadRequest("Invalid Port");

            if (string.IsNullOrWhiteSpace(model.Password_Hash))
                return BadRequest("Password is required");

            if (!IsValidEmailList(model.From_Mail))
                return BadRequest("Invalid From Mail");

            if (!IsValidEmailList(model.To_Mail))
                return BadRequest("Invalid To Mail");

            if (!IsValidEmailList(model.CC_Mail))
            {
                return BadRequest("Invalid CC Mail");
            }
            var fromMail = model.From_Mail?.Trim().ToLower();

            var toEmails = GetEmailList(model.To_Mail);

            var ccEmails = GetEmailList(model.CC_Mail);

            if (toEmails.Contains(fromMail))
                return BadRequest("From Email cannot exist in To Email");

            if (ccEmails.Contains(fromMail))
                return BadRequest("From Email cannot exist in CC Email");

            if (HasDuplicateEmails(model.To_Mail))
                return BadRequest("Duplicate emails found in To Email");

            if (HasDuplicateEmails(model.CC_Mail))
                return BadRequest("Duplicate emails found in CC Email");

            if (toEmails.Intersect(ccEmails).Any())
                return BadRequest("Same email exists in To and CC");

            model.Created_On = DateTime.Now;
            model.Is_Active = true;

            _context.MAIL_SETTINGS.Add(model);

            await _context.SaveChangesAsync();

            return Ok(model);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            MailSettings model)
        {
            var mail = await _context.MAIL_SETTINGS
                .FirstOrDefaultAsync(x =>
                    x.Mail_Setting_ID == id);

            if (mail == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(model.Host))
                return BadRequest("Host is required");

            if (model.Port <= 0)
                return BadRequest("Invalid Port");

            if (string.IsNullOrWhiteSpace(model.Password_Hash))
                return BadRequest("Password is required");

            if (!IsValidEmailList(model.From_Mail))
                return BadRequest("Invalid From Mail");

            if (!IsValidEmailList(model.To_Mail))
                return BadRequest("Invalid To Mail");

            if (!IsValidEmailList(model.CC_Mail))
                return BadRequest("Invalid CC Mail");

            var fromMail = model.From_Mail?.Trim().ToLower();

            var toEmails = GetEmailList(model.To_Mail);

            var ccEmails = GetEmailList(model.CC_Mail);

            // From Mail cannot exist in To Mail
            if (toEmails.Contains(fromMail))
                return BadRequest("From Email cannot exist in To Email");

            // From Mail cannot exist in CC Mail
            if (ccEmails.Contains(fromMail))
                return BadRequest("From Email cannot exist in CC Email");

            // Duplicate To Emails
            if (HasDuplicateEmails(model.To_Mail))
                return BadRequest("Duplicate emails found in To Email");

            // Duplicate CC Emails
            if (HasDuplicateEmails(model.CC_Mail))
                return BadRequest("Duplicate emails found in CC Email");

            // Same email in To and CC
            if (toEmails.Intersect(ccEmails).Any())
                return BadRequest("Same email exists in To and CC");

            mail.Host = model.Host;
            mail.Port = model.Port;
            mail.From_Mail = model.From_Mail;
            mail.Password_Hash = model.Password_Hash;
            mail.To_Mail = model.To_Mail;
            mail.CC_Mail = model.CC_Mail;
            mail.Modified_On = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok("Updated Successfully");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var mail = await _context.MAIL_SETTINGS
                .FirstOrDefaultAsync(x => x.Mail_Setting_ID == id);

            if (mail == null)
                return NotFound();

            mail.Is_Active = false;

            await _context.SaveChangesAsync();

            return Ok();
        }

        private bool IsValidEmailList(string emails)
        {
            if (string.IsNullOrWhiteSpace(emails))
                return true;

            var emailList = emails.Split(',');

            foreach (var email in emailList)
            {
                try
                {
                    var addr = new MailAddress(email.Trim());
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }

        private List<string> GetEmailList(string emails)
        {
            if (string.IsNullOrWhiteSpace(emails))
                return new List<string>();

            return emails.Split(',')
                .Select(x => x.Trim().ToLower())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        private bool HasDuplicateEmails(string emails)
        {
            var list = GetEmailList(emails);

            return list.Count != list.Distinct().Count();
        }
    }
}
