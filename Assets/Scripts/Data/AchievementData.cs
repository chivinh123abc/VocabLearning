using UnityEngine;

[CreateAssetMenu(fileName = "AchievementData", menuName = "VocabLearning/Achievement")]
public class AchievementData : ScriptableObject
{
    public string id;
    public string title;
    public string description;
    public string iconText;
    public int rewardCoins;
    public int rewardExp;
}