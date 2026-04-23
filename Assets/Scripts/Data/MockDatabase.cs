using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace VocabLearning.Data
{
    [System.Serializable]
    public class MockDatabase
    {
        public UserJson currentUser;
        public List<VocabSetJson> vocabSets;
        public List<AchievementData> achievements; 
        public List<UserAchievementData> userAchievements;
        public List<ItemData> items;
        public List<UserItemData> userItems;
    }

    [System.Serializable]
    public class UserJson
    {
        public string id;
        public string username;
        public string avatar;
        public string email;
        public int level;
        public int exp;
        public int expNeeded;
        public int coins;
        public string rank;
        public int rankPoints;
        public List<string> learnedSets = new List<string>();
        public List<UserItemData> userItems; 
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
        public List<WordJson> words;
    }

    [System.Serializable]
    public class WordJson
    {
        public string id;
        public string word;
        public string meaning;
    }
    [System.Serializable]
    public class AchievementData 
    {
       public int achievementId;
       public string name;
       public string description;
       public int rewardCoins;
    }

    [System.Serializable]
    public class UserAchievementData
    {
       public string userId;
       public int achievementId;
       public string achievedAt;
    }

    [System.Serializable]
    public class ItemData
    {
      public int itemId;
      public string name;
      public int price;
      public string itemType; // e.g., "Avatar", "Background", "theme"
      public string imageUrl; 
      public string description;
    }

    [System.Serializable]
    public class UserItemData
    {
      public string userId;
      public int itemId;
      public int quantity;
    }
}
