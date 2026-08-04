using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DFN_BMS.Models
{
    [Table("PRICE_MASTER")]
    public class PriceMaster
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PartNumberId { get; set; }   // FK -> ItemMaster.Id

        [ForeignKey("PartNumberId")]
        [ValidateNever]
        public ItemMaster? PartNumber { get; set; }

        [Required]
        [MaxLength(50)]
        public string GroupCode { get; set; }

        [Required]
        [MaxLength(20)]
        public string CustomerOrSupplier { get; set; }   // "Customer" or "Supplier"

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Rate { get; set; }

        [Required]
        public DateTime EffectiveDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }
    }
}