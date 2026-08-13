using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DFN_BMS.Models
{
    [Table("UOM_MASTER")]
    public class UomMaster
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string UomName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}