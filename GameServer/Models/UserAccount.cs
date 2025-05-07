namespace GameServer.Models
{
    public class UserAccount
    {
        public long UserUniqueID { get; set; }
        public string ID { get; set; }
        public string Password { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? Nickname { get; set; }

        // Navigation Properties
        public List<UserCharacterOverview> Characters { get; set; } = new();
        public List<CharacterInventory> Inventories { get; set; } = new();
        public List<UserCharacterStatus> CharacterStatuses { get; set; } = new();
        public List<PlayerLocation> PlayerLocations { get; set; } = new();
    }
}