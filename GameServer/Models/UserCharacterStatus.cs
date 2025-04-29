namespace GameServer.Models
{
    public class UserCharacterStatus
    {
        public long CharacterUniqueID { get; set; }
        public long UserUniqueID { get; set; }
        public string? CharacterNickname { get; set; }
        public int? CharacterLevel { get; set; }
        public double? CharacterHP { get; set; }
        public double? CharacterMP { get; set; }
        public double? CharacterStamina { get; set; }
        public double? Gold { get; set; }
        public long? CharacterExp { get; set; }

        // Navigation
        public UserCharacterOverview Character { get; set; }
        public UserAccount UserAccount { get; set; }
    }
}