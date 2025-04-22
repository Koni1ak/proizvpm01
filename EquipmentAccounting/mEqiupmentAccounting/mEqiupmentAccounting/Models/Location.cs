using Postgrest.Attributes;
using Postgrest.Models;

namespace mEquipmentAccounting.Models
{
    [Table("locations")]
    public class Location : BaseModel
    {
        [PrimaryKey("location_id", false)]
        public long LocationId { get; set; }

        [Column("location_name")]
        public string LocationName { get; set; }

        [Column("address")]
        public string Address { get; set; }

        [Column("description")]
        public string Description { get; set; }
    }
}