using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DFN_BMS.Models
{
    [Table("ITEM_TYPE_MASTER")]
    public class ItemTypeMaster
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string TypeName { get; set; }

        public bool IsActive { get; set; } = true;
    }
}