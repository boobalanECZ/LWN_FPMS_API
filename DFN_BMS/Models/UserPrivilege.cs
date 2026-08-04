using System.ComponentModel.DataAnnotations.Schema;

namespace DFN_BMS.Models
{
    [Table("USER_PRIVILEGES")]
    public class UserPrivilege
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? MenuName { get; set; }
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }
}
