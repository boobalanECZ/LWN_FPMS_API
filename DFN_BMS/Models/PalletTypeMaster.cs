using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DFN_BMS.Models
{
    [Table("PALLET_TYPE_MASTER")]
    // Reference table for pallet card series, e.g. "IN" ranges 01-30,
    // "BR" ranges 01-10. CurrentSequence tracks the last number issued
    // for this series so the next Store Master save can continue from
    // there, wrapping back to 1 once RangeTo is reached.
    public class PalletTypeMaster
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(10)]
        public string PalletName { get; set; }   // e.g. "IN", "EX", "N3IN"

        [Required]
        public int RangeFrom { get; set; } = 1;

        [Required]
        public int RangeTo { get; set; }

        // Last sequence number issued. Server-managed — the client never
        // sends this.
        [ValidateNever]
        public int CurrentSequence { get; set; } = 0;
    }
}