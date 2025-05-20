namespace GameServer.Models { 
    public record RegisterRequest(string ID, string Password, string Email, string Name, string Nickname);
}