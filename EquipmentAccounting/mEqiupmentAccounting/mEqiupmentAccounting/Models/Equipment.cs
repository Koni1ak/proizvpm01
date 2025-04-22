using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace mEquipmentAccounting.Models
{
    [Table("equipment")]
    public class Equipment : BaseModel
    {
        [PrimaryKey("equipment_id", false)]
        public long EquipmentId { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("serial_number")]
        public string SerialNumber { get; set; }

        [Column("inventory_number")]
        public string InventoryNumber { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("purchase_date")]
        public DateTime? PurchaseDate { get; set; }

        [Column("warranty_expiry_date")]
        public DateTime? WarrantyExpiryDate { get; set; }

        [Column("category_id")]
        public long CategoryId { get; set; }

        [Reference(typeof(Category))]
        public Category Category { get; set; }

        [Column("status_id")]
        public long StatusId { get; set; }

        [Reference(typeof(Status))]
        public Status Status { get; set; }

        [Column("location_id")]
        public long LocationId { get; set; }

        [Reference(typeof(Location))]
        public Location Location { get; set; }

        [Column("assigned_user_id")]
        public long? AssignedUserId { get; set; }

        [Reference(typeof(User), true)] 
        public User AssignedUser { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }
    }
}