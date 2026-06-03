namespace SpotifyRecommender.Models;

public class DatabaseUser
{
    public DatabaseUser()
    {
        
    }
    public string id { get; set; }
    public string displayName { get; set; }
    public string refreshToken { get; set; }
    public string lastLogin { get; set; }
}