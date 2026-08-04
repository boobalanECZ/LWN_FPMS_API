using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DFN_BMS.Models
{
    [Table("ITEM_MASTER")]
    public class ItemMaster
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        [ValidateNever]
        public string ItemNumber { get; set; }   // auto-generated, e.g. ITM-0001

        [Required]
        [MaxLength(150)]
        public string ItemName { get; set; }

        [Required]
        public int ItemTypeId { get; set; }

        [ForeignKey("ItemTypeId")]
        [ValidateNever]
        public ItemTypeMaster? ItemType { get; set; }

        // Transient — NOT stored on this table. The frontend's Item Type
        // CreatableSelect always sends this (whether the person picked an
        // existing type or typed a brand new one). The controller resolves
        // it to an ItemTypeId the same way UsersController resolves
        // DepartmentName -> DepartmentId, creating the type if it's new.
        [NotMapped]
        public string ItemTypeName { get; set; }

        [Required]
        public int ItemGroupId { get; set; }

        [ForeignKey("ItemGroupId")]
        [ValidateNever]
        public ItemGroupMaster? ItemGroup { get; set; }

        [MaxLength(20)]
        public string HsnCode { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [MaxLength(20)]
        public string Uom { get; set; }           // free text now, no fixed list

        [Column(TypeName = "decimal(18,3)")]
        public decimal? WeightPerUnit { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal? StuffQuantity { get; set; }

        [MaxLength(100)]
        public string ItemModel { get; set; }

        [MaxLength(30)]
        public string Usage { get; set; }         // free text now, no fixed list

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Length { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Width { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Height { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal SafetyLevel { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal ReorderLevel { get; set; }

        [Required]
        [MaxLength(20)]
        public string DangerLevel { get; set; }   // free text now, no fixed list

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }
    }
}