using UnityEngine.UIElements;
using System;
using UnityEngine;
using Unity.VisualScripting.FullSerializer;
using System.Linq;
using NUnit.Framework;
using System.Runtime.InteropServices;

namespace VocabLearning.UI
{
    public class AchievementController
    {
        private VisualElement _root;
        private Action<VisualTreeAsset> _onNavigate;
        private VocabLearning.Data.MockDatabase _db;

        public AchievementController(VisualElement root, VocabLearning.Data.MockDatabase db, Action<VisualTreeAsset> onNavigate)
        {
            _root = root;
            _db = db;
            _onNavigate = onNavigate;
        }

        public void Bind(VisualTreeAsset profileAsset)
        {    
           var allAchievements = _db.achievements;
           var userUnlocked = _db.userAchievements;

           int totalUnlocked = 0;

           _root.Q<Button>("btnBack")?.RegisterCallback<ClickEvent>(evt => _onNavigate?.Invoke(profileAsset));
           
           foreach (var data in allAchievements)
            {
                var card = _root.Q<VisualElement>($"Card_{data.achievementId}");
                if (card != null)
                {
                    var lblTitle = card.Q<Label>("Lbl_Title");
                    var lblDesc = card.Q<Label>("Lbl_Desc");
                    var lblCoins = card.Q<Label>("Lbl_Coins");

                    if (lblTitle != null) lblTitle.text = data.name;
                    if (lblDesc != null) lblDesc.text = data.description;
                    if (lblCoins != null) lblCoins.text = data.rewardCoins.ToString();

                    //Kiem tra mo khoac 
                    bool isUnlocked = userUnlocked.Any(ua => ua.achievementId == data.achievementId);
                    
                    Debug.Log($"Achievement {data.achievementId} - Unlock: " + 
                          userUnlocked.Any(ua => ua.achievementId == data.achievementId));

                    if (isUnlocked)
                    {
                        totalUnlocked++;
                        UpdataAchievementCard( card, true);   
                    }
                    else
                    {
                        UpdataAchievementCard(card, false);
                    }
                }
            }

            var lblSummary = _root.Q<Label>("Lbl_AchievementSummary");
            if (lblSummary != null)
            {
                lblSummary.text = $"{totalUnlocked}/{allAchievements.Count} unlocked";
            }
            
           
        }

        private void UpdataAchievementCard(VisualElement  card , bool isUnlocked)
        {
            if(card == null) return;

            var unLockGroup = card.Q<VisualElement>("UnLock_UI");
            var lockGroup = card.Q<VisualElement>("Lock_UI");

            if (isUnlocked)
            {
                if (unLockGroup != null) unLockGroup.style.display = DisplayStyle.Flex; 
                Debug.Log(unLockGroup == null ? "UnLock_UI NULL" : "UnLock_UI OK");
                if (lockGroup != null) lockGroup.style.display = DisplayStyle.None;
                Debug.Log(lockGroup == null ? "Lock_UI NULL" : "Lock_UI OK");
                card.style.opacity = 1.0f;
            }
            else
            {
                if (unLockGroup != null) unLockGroup.style.display = DisplayStyle.None;
                if (lockGroup != null) lockGroup.style.display = DisplayStyle.Flex;
               card.style.opacity = 0.7f;
            }
        }

       
    }
}