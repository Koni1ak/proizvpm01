using Postgrest.Attributes;
using Postgrest.Models;

namespace mEquipmentAccounting.Models
{
    [Table("categories")]
    public class Category : BaseModel
    {
        [PrimaryKey("category_id", false)]
        public long CategoryId { get; set; }

        [Column("category_name")]
        public string CategoryName { get; set; }

        [Column("description")]
        public string Description { get; set; }
    }
}