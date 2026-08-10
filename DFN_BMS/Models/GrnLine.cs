using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DFN_BMS.Models
{
    [Table("GRN_LINE")]
    public class GrnLine
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GrnHeaderId { get; set; }   
        [ForeignKey("GrnHeaderId")]
        [ValidateNever]
        public GrnHeader? Header { get; set; }

        [Required]
        public int ItemId { get; set; }   

        [ForeignKey("ItemId")]
        [ValidateNever]
        public ItemMaster? Item { get; set; }

        [MaxLength(20)]
        public string Uom { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal? PalletQuantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Rate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalValue { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}