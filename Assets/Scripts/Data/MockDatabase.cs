using System.Collections.Generic;
using UnityEngine;

namespace VocabLearning.Data
{
    [System.Serializable]
    public class MockDatabase
    {
        public UserJson currentUser;
        public List<VocabSetJson> vocabSets;
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
        public List<WordJson> words;
    }

    [System.Serializable]
    public class WordJson
    {
        public string id;
        public string word;
        public string meaning;
    }
}
