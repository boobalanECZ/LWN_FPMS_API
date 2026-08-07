
using System.ComponentModel.DataAnnotations.Schema;
[Table("ITEM_GROUP_MASTER")]
public class ItemGroupMaster
{
   
    public int Id { get; set; }

    public string? GroupName { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }
}