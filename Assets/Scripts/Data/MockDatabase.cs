using System.Collections.Generic;
using UnityEngine;

namespace VocabLearning.Data
{
    [System.Serializable]
    public class MockDatabase
    {
        public UserJson currentUser;
        public List<WordJson> words;        // Bảng từ vựng trung tâm
        public List<VocabSetJson> vocabSets; // Bộ từ vựng (chỉ lưu wordIds)
        public List<AchievementJson> achievements;
        public List<InventoryItemJson> inventory;
        public List<QuestJson> quests;
        public List<QuestJson> questPool; // NEW: Pool of all possible quests
        public List<ShopItemJson> shopItems;
        public List<UserJson> leaderboardUsers;
    }

    [System.Serializable]
    public class UserJson
    {
        public string id;
        public string username;
        public string email;
        public int level;
        public int exp;
        public int expNeeded;
        public int coins;
        public string rank;
        public int rankPoints;
        public int wins;
        public int totalGames;
        public string lastQuestRefreshDate;
        public WeeklyLoginData weeklyLogin; // NEW: Weekly login tracking
        public List<string> learnedSets = new List<string>();
    }

    [System.Serializable]
    public class VocabSetJson
    {
        public string id;
        public string title;
        public string description;
        public int wordCount;
        public string category;
        public string difficulty;
        public string rankRequired;
        public List<string> wordIds; // ID của các từ trong bảng words
    }

    [System.Serializable]
    public class WordJson
    {
        public string id;
        public string word;
        public string meaning;
        public string rankRequired; // Rank tối thiểu: Dong, Bac, Vang, BachKim, KimCuong, SieuCap
    }

    [System.Serializable]
    public class AchievementJson
    {
        public string id;
        public string icon;
        public string title;
        public string description;
        public int maxProgress;
        public int currentProgress;
        public bool isUnlocked;
        public string unlockDate;
    }

    [System.Serializable]
    public class InventoryItemJson
    {
        public string id;
        public string icon;
        public string name;
        public string description;
        public int quantity;
        public string rarity; // Common, Rare, Epic, Legendary
        public string category; // Consumable, Cosmetic
        public string equipType; // Avatar, Border, Effect (only for Cosmetics)
        public bool isEquipped; // Relevant for Cosmetic
        public bool isCombatItem; // Chỉ dùng cho item trong Battle
    }

    [System.Serializable]
    public class QuestJson
    {
        public string id;
        public string title;
        public string description;
        public int currentProgress;
        public int maxProgress;
        public int rewardCoins;
        public int rewardExp;
        public bool isClaimed;
        public string questType; // WinRanked, WinBattle, LearnWord, etc.
    }

    [System.Serializable]
    public class WeeklyLoginData
    {
        public string weekStartDate;    // Ngày bắt đầu tuần (ví dụ Thứ 2)
        public List<string> loginDates = new List<string>(); // Danh sách các ngày đã đăng nhập trong tuần
        public bool isRewardClaimed;
    }

    [System.Serializable]
    public class ShopItemJson
    {
        public string id;
        public string name;
        public string description;
        public string icon;
        public int price;
        public string rarity; // Common, Rare, Epic, Legendary
        public string category; // Consumable, Cosmetic, Theme
        public string equipType; // Avatar, Border, Effect (only for Cosmetics)
    }
}
