namespace GameServer.Models
{
    public class PlayerLocation
    {
        public long CharacterUniqueID { get; set; }
        public long UserUniqueID { get; set; }
        public double? PositionX { get; set; }
        public double? PositionY { get; set; }

        // Navigation
        public UserCharacterOverview Character { get; set; }
        public UserAccount UserAccount { get; set; }
    }
}