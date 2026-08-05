using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        [MaxLength(30)]
        public string PalletNumber { get; set; }   // manually entered by the user

        [Required]
        [MaxLength(7)]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Colour must be a valid hex code (e.g. #1E88E5)")]
        public string ColourCode { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }
    }
}