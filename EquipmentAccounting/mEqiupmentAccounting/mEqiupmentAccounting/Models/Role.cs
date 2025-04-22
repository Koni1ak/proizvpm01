using Postgrest.Attributes;
using Postgrest.Models;

namespace mEquipmentAccounting.Models
{
    [Table("roles")]
    public class Role : BaseModel
    {
        [PrimaryKey("role_id", false)] 
        public long RoleId { get; set; }

        [Column("role_name")]
        public string RoleName { get; set; }

        [Column("description")]
        public string Description { get; set; }
    }
}