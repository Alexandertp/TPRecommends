using System;
using SpotifyRecommender.Models;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.IO;
using SQLitePCL;

namespace SpotifyRecommender.Services;
//Alt i dette dokument er skrevet i hånden, uden brug af AI
public class DatabaseService
{
    private readonly SqliteConnection _connection;

    public DatabaseService()
    {
        //Sindssyg linje for at få projekt root, så SQL kan blive opbevaret et let-tilgængeligt sted.
        string folder = Environment.CurrentDirectory + "/Sqlite/";
        CreateDBFolder(folder);
        _connection = new SqliteConnection($"Data Source={folder}SpotifyDB.sqlite");
        _connection.Open();
        CreateDBTables();
    }

    public void CreateDBFolder(string DBFolder)
    {
        if (!Directory.Exists(DBFolder))
        {
            Directory.CreateDirectory(DBFolder);
        }
    }

    public void CreateDBTables()
    {
        
        var command = _connection.CreateCommand();

        string sql = @"CREATE TABLE IF NOT EXISTS users(
        id TEXT PRIMARY KEY,
        display_name TEXT NOT NULL,
        refresh_token TEXT NOT NULL,
        last_login TEXT NOT NULL);";
        command.CommandText = sql;
        command.ExecuteNonQuery();
        //List_name viser om en sang er trukket fra recently played eller anbefalet af appen
        sql = @"CREATE TABLE IF NOT EXISTS tracks(
        user_id TEXT NOT NULL REFERENCES users(id),
        song_name TEXT NOT NULL,
        position INTEGER NOT NULL,
        PRIMARY KEY(user_id, song_name,position));";
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
    
    public void CreateUser(string id, string displayName, string refreshToken, string lastLogin)
    {
        var command = _connection.CreateCommand();
        command.CommandText = @"INSERT INTO users(id, display_name,refresh_token,last_login) VALUES($id, $display_name, $refresh_token, $last_login);";
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$display_name", displayName);
        command.Parameters.AddWithValue("$refresh_token", refreshToken);
        command.Parameters.AddWithValue("$last_login", lastLogin);
        command.ExecuteNonQuery();
    }
    public void CreateUser(DatabaseUser dbUser)
    {
        CreateUser(dbUser.id, dbUser.displayName, dbUser.refreshToken, dbUser.lastLogin);
    }

    public bool doesUserExist()
    {
        var command = _connection.CreateCommand();
        command.CommandText = @"SELECT * FROM users;";
        var reader = command.ExecuteReader();
        return reader.Read();
    }

    public DatabaseUser getUserFromDB()
    {
        var command = _connection.CreateCommand();
        command.CommandText = @"SELECT * FROM users LIMIT 1;";
        var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }
        DatabaseUser newDBUser = new DatabaseUser();
        newDBUser.id = reader["id"].ToString();
        newDBUser.displayName = reader["display_name"].ToString();
        newDBUser.refreshToken = reader["refresh_token"].ToString();
        newDBUser.lastLogin = reader["last_login"].ToString();
        
        return newDBUser;
    }

    public void updateLoginOnCurrentUser(DatabaseUser currentUser)
    {
        var command = _connection.CreateCommand();
        command.CommandText =
            $@"UPDATE users SET refresh_token = '{currentUser.refreshToken}', last_login = '{currentUser.lastLogin}' WHERE id = '{currentUser.id}'";
        command.ExecuteNonQuery();
    }

    public void SaveRecentTracks(List<SpotifyTrack> tracks, string userId)
    {
        var command = _connection.CreateCommand();
        command.CommandText = $@"DELETE FROM tracks where user_id = '{userId}';";
        command.CommandText += $@"INSERT INTO tracks (user_id, song_name, position) VALUES ";
        for (int i = 0; i < tracks.Count; i++)
        {
            command.CommandText += $@"('{userId}','{tracks[i].Name.Replace("'",@"''")}',{i})";
            if (i != tracks.Count - 1)
            {
                command.CommandText += ", ";
                
            }
        }
        command.ExecuteNonQuery();
    }

    public bool HasRecentTracks(string userId)
    {
        var command = _connection.CreateCommand();
        command.CommandText = $@"SELECT * FROM tracks  WHERE user_id = '{userId}';";
        var reader = command.ExecuteReader();
        return reader.Read();
    }
    
    public List<DatabaseTrack> LoadRecentTracks(string userId)
    {
        var command = _connection.CreateCommand();
        command.CommandText = $@"SELECT * FROM tracks WHERE user_id = '{userId}' ORDER BY position;";
        var reader = command.ExecuteReader();

        List<DatabaseTrack> SongsOut = new List<DatabaseTrack>();
        while (reader.Read())
        {
            DatabaseTrack newTrack = new DatabaseTrack();
            newTrack.name = reader["song_name"].ToString();
            SongsOut.Add(newTrack);
        }

        return SongsOut;
    } 
    /*
    public List<SpotifyTrack> getRecentTracksFromDB(string id)
    {
        var command = _connection.CreateCommand();
        command.CommandText = @$"SELECT * FROM tracks WHERE user_id LIKE {id};";
        var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }
    }
    */
    
}