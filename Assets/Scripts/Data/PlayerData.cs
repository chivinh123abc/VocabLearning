using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public string id = "player_001";
    public string username = "Player";
    public string email = "";

    public int level = 1;
    public int exp = 0;
    public int expNeeded = 100;
    public int coins = 0;
    // Rank system
    public string rank = "Beginner";
    public int rankPoints = 0;
    // Statistics
    public int totalGames = 0;
    public int wins = 0;
    public int learnedWords = 0;
    public int achievements = 0;
}