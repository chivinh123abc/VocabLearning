using UnityEngine;

[CreateAssetMenu(fileName = "QuestData", menuName = "VocabLearning/Quest")]
public class QuestData : ScriptableObject
{
    public string id;
    public string title;
    public string description;
    public int total;
    public int rewardCoins;
    public int rewardExp;
}