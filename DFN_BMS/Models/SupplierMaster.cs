using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DFN_BMS.Models
{
    [Table("SUPPLIER_MASTER")]
    public class SupplierMaster
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string SupplierCode { get; set; }   // manually entered by the user

        [Required]
        [MaxLength(150)]
        public string SupplierName { get; set; }

        [Required]
        [MaxLength(50)]
        public string VendorCode { get; set; }

        [Required]
        public int SupplierGroupId { get; set; }

        [ForeignKey("SupplierGroupId")]
        [ValidateNever]
        public SupplierGroupMaster? SupplierGroup { get; set; }

        [Required]
        [MaxLength(100)]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string Email { get; set; }

        [Required]
        [MaxLength(10)]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Contact Number must be exactly 10 digits")]
        public string ContactNumber { get; set; }

        [Required]
        [MaxLength(100)]
        public string PersonToContact { get; set; }

        [Required]
        [MaxLength(15)]
        [RegularExpression(
            @"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$",
            ErrorMessage = "Enter a valid 15-character GSTIN (e.g. 33ABCDE1234F1Z5)")]
        public string GstNo { get; set; }

        [Required]
        [MaxLength(10)]
        [RegularExpression(
            @"^[A-Z]{5}[0-9]{4}[A-Z]{1}$",
            ErrorMessage = "Enter a valid 10-character PAN (e.g. ABCDE1234F)")]
        public string PanNo { get; set; }

        // ---------- Billing Address ----------
        [Required]
        [MaxLength(150)]
        public string BillingCompanyName { get; set; }

        [Required]
        [MaxLength(200)]
        public string BillingAddressLine1 { get; set; }

        [MaxLength(200)]
        public string BillingAddressLine2 { get; set; }

        [Required]
        [MaxLength(50)]
        public string BillingState { get; set; }

        [Required]
        [MaxLength(10)]
        public string BillingStateCode { get; set; }

        [Required]
        [MaxLength(10)]
        [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "Pin Code must be exactly 6 digits")]
        public string BillingPinCode { get; set; }

        // ---------- Shipping Address ----------
        [Required]
        [MaxLength(150)]
        public string ShippingCompanyName { get; set; }

        [Required]
        [MaxLength(200)]
        public string ShippingAddressLine1 { get; set; }

        [MaxLength(200)]
        public string ShippingAddressLine2 { get; set; }

        [Required]
        [MaxLength(50)]
        public string ShippingState { get; set; }

        [Required]
        [MaxLength(10)]
        public string ShippingStateCode { get; set; }

        [Required]
        [MaxLength(10)]
        [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "Pin Code must be exactly 6 digits")]
        public string ShippingPinCode { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }
    }
}