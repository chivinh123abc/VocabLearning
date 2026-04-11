using System.Collections.Generic;
using UnityEngine;

namespace VocabLearning.Data
{
    [CreateAssetMenu(fileName = "VocabSetData", menuName = "VocabLearning/Vocabulary Set")]
    public class VocabSetData : ScriptableObject
    {
        public string id;
        public string title;
        public string description;
        public int wordCount;
        public string category;
        public string difficulty;
        public Sprite image;
        public List<WordData> words;
    }
}