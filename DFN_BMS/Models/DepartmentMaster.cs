using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DFN_BMS.Models
{
    [Table("DEPARTMENT_MASTER")]
    public class DepartmentMaster
    {
        [Key]
        public int Id { get; set; }
        public string? DepName { get; set; }
        public bool IsActive { get; set; }
    }
}
