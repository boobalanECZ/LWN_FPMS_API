using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace DFN_BMS.Models
{
    [Table("ITEM_GROUP_MASTER")]
    public class ItemGroupMaster
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string GroupCode { get; set; }

        [Required]
        [MaxLength(100)]
        public string GroupName { get; set; }

        [MaxLength(250)]
        public string Description { get; set; }

        // true = Active, false = Inactive
        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }
    }
}