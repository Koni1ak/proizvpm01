using Postgrest.Attributes;
using Postgrest.Models;

namespace mEquipmentAccounting.Models
{
    [Table("statuses")]
    public class Status : BaseModel
    {
        [PrimaryKey("status_id", false)]
        public long StatusId { get; set; }

        [Column("status_name")]
        public string StatusName { get; set; }

        [Column("description")]
        public string Description { get; set; }
    }
}