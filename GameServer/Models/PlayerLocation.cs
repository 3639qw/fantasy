using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServer.Models
{
    [Table("UserSceneData")]
    public class UserSceneData
    {
        [Key]
        public int ID { get; set; }

        public int UserUniqueID { get; set; }
        public string SceneName { get; set; } = "";
        public float PositionX { get; set; }
        public float PositionY { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
