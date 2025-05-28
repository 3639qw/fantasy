namespace GameServer.Models
{
    public class CharacterInventory
    {
        public long CharacterUniqueID { get; set; }
        public long UserUniqueID { get; set; }
        public int InventoryNumber { get; set; }
        public string? ItemName { get; set; }

        // Navigation
        public UserCharacterOverview Character { get; set; }
        public UserAccount UserAccount { get; set; }
    }
}