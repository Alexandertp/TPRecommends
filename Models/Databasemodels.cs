namespace SpotifyRecommender.Models;
//Alt i dette dokument er skrevet i hånden
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

public class DatabaseTrack
{
    public DatabaseTrack()
    {
        
    }
    public string name { get; set; }
    
}