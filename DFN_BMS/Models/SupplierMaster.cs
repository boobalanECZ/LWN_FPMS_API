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
        [ValidateNever]
        public string SupplierCode { get; set; }   // auto-generated, e.g. SUP-0001

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
        public string Email { get; set; }

        [Required]
        [MaxLength(20)]
        public string ContactNumber { get; set; }

        [Required]
        [MaxLength(100)]
        public string PersonToContact { get; set; }

        [Required]
        [MaxLength(20)]
        public string GstNo { get; set; }

        [Required]
        [MaxLength(20)]
        public string PanNo { get; set; }

        // ---------- Billing Address ----------
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
        public string BillingPinCode { get; set; }

        // ---------- Shipping Address ----------
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
        public string ShippingPinCode { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }
    }
}