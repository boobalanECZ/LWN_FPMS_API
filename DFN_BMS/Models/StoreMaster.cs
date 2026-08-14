using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DFN_BMS.Models
{
    [Table("STORE_MASTER")]
    public class StoreMaster
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string StoreLocation { get; set; }

        [Required]
        public int PalletTypeId { get; set; }   // FK -> PalletTypeMaster.Id

        [ForeignKey("PalletTypeId")]
        [ValidateNever]
        public PalletTypeMaster? PalletType { get; set; }

        [MaxLength(30)]
        [ValidateNever]                         // server-generated, e.g. "IN-01" — don't validate on input
        public string? PalletNumber { get; set; }

        [Required]
        [MaxLength(7)]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Colour must be a valid hex code (e.g. #1E88E5)")]
        public string ColourCode { get; set; }

        // Part Number dropdown on the Store Master screen.
        // Nullable so existing rows saved before this field existed still load fine.
        public int? PartNumberId { get; set; }   // FK -> ItemMaster.Id

        [ForeignKey("PartNumberId")]
        [ValidateNever]
        public ItemMaster? PartNumber { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
    }
}