namespace GameServer.Models
{
    public class UserCharacterOverview
    {
        public long CharacterUniqueID { get; set; }
        public long UserUniqueID { get; set; }
        public string? CharacterNickname { get; set; }
        public int? CharacterSlotNumber { get; set; }

        // Navigation
        public UserAccount UserAccount { get; set; }

        public List<CharacterInventory> Inventories { get; set; } = new();
        public UserCharacterStatus CharacterStatus { get; set; }
        public PlayerLocation PlayerLocation { get; set; }
    }
}