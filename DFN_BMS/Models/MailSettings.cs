using System.ComponentModel.DataAnnotations;

namespace DFN_BMS.Models
{
    public class MailSettings
    {
        [Key]
        public int Mail_Setting_ID { get; set; }
        public string? Host { get; set; }
        public int Port { get; set; }
        public string? From_Mail { get; set; }
        public string? Password_Hash { get; set; }
        public string? To_Mail { get; set; }
        public string? CC_Mail { get; set; }
        public bool Is_Active { get; set; }
        public int? Created_By { get; set; }
        public DateTime? Created_On { get; set; }
        public int? Modified_By { get; set; }
        public DateTime? Modified_On { get; set; }
    }
}