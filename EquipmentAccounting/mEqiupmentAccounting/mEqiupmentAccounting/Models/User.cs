using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace mEquipmentAccounting.Models
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
        public long? RoleId { get; set; } // Делаем Nullable на всякий случай

        // Ссылка для загрузки роли (опционально, если нужно в деталях пользователя)
        [Reference(typeof(Role), true)] // true - игнорировать Null FK
        public Role Role { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        // Удобное свойство для отображения
        public string FullName => $"{LastName} {FirstName}".Trim();
        public string DisplayName => !string.IsNullOrWhiteSpace(FullName) ? FullName : Username;
    }
}