using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace EquipmentAccounting.Models
{
    [Table("assignment_history")]
    public class AssignmentHistory : BaseModel
    {
        [PrimaryKey("history_id", false)]
        public long HistoryId { get; set; }

        [Column("equipment_id")]
        public long EquipmentId { get; set; }

        [Column("changed_by_user_id")]
        public long ChangedByUserId { get; set; }

        [Column("change_timestamp")]
        public DateTimeOffset ChangeTimestamp { get; set; }

        [Column("change_type")]
        public string ChangeType { get; set; } 

        [Column("previous_status_id")]
        public long? PreviousStatusId { get; set; }

        [Column("new_status_id")]
        public long? NewStatusId { get; set; }

        [Column("previous_location_id")]
        public long? PreviousLocationId { get; set; }

        [Column("new_location_id")]
        public long? NewLocationId { get; set; }

        [Column("previous_assigned_user_id")]
        public long? PreviousAssignedUserId { get; set; }

        [Column("new_assigned_user_id")]
        public long? NewAssignedUserId { get; set; }

        [Column("notes")]
        public string Notes { get; set; }
    }
}