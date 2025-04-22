using Postgrest.Attributes;
using Postgrest.Models;
using System;
using System.Collections.Generic;

namespace EquipmentAccounting.Models
{
    [Table("users")]
    public class User : BaseModel
    {
        [PrimaryKey("user_id", false)]
        public long UserId { get; set; }

        [Column("username")]
        public string Username { get; set; }



        [Column("first_name")]
        public string FirstName { get; set; }

        [Column("last_name")]
        public string LastName { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("role_id")]
        public long RoleId { get; set; }

        [Reference(typeof(Role))] 
        public Role Role { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        
        public string FullName => $"{FirstName} {LastName}";
    }
}