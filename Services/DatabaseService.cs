using System;
using SpotifyRecommender.Models;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.IO;
using SQLitePCL;

namespace SpotifyRecommender.Services;

public class DatabaseService
{
    private readonly SqliteConnection _connection;

    public DatabaseService(string sqlConnection)
    {
        //Sindssyg linje for at få projekt root, så SQL kan blive opbevaret et let-tilgængeligt sted.
        string folder = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + "/Sqlite/";
        string dbPath = Path.Combine(folder, "data.db");
        _connection = new SqliteConnection($"Data Source={folder}SpotifyDB.sqlite");
        _connection.ConnectionString = sqlConnection;
    }

    public async Task CreateDBTables()
    {
        _connection.Open();
        string sql = @"CREATE TABLE IF NOT EXISTS users(
        id INTEGER PRIMARY KEY,
        display_name TEXT NOT NULL,
        refresh_token TEXT NOT NULL,
        last_login TEXT NOT NULL);";
        //List_name viser om en sang er trukket fra recently played eller anbefalet af appen
        sql += @"CREATE TABLE IF NOT EXISTS tracks(
        user_id TEXT NOT NULL REFERENCES users(id),
        list_name TEXT NOT NULL,
        position INTEGER NOT NULL,
        track_json TEXT NOT NULL,
        saved_at TEXT NOT NULL,
        PRIMARY KEY(user_id, list_name,position)";
    }
    
    public async Task CreateUser()
    {
        _connection.Open();
        var command = "";
    }
}