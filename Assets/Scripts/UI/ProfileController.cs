using UnityEngine.UIElements;
using System;
using UnityEngine;

namespace VocabLearning.UI
{
    public class ProfileController 
    {   
        private VisualElement _root;
        private Action<VisualTreeAsset> _onNavigate;
        private VocabLearning.Data.MockDatabase _db;

        public ProfileController(VisualElement root, VocabLearning.Data.MockDatabase db, Action<VisualTreeAsset> onNavigate)
        {
            _root = root;
            _db = db;
            _onNavigate = onNavigate;
        }

        public void Bind(VisualTreeAsset achievementAsset, VisualTreeAsset settingAsset , VisualTreeAsset InventoryAsset)
        {
            if (_db == null || _db.currentUser == null) return;
            var user = _db.currentUser;
            
            // Tên và Level
            SetText("Lbl_PlayerName", user.username);
            SetText("Lbl_PlayerLevel", $"Level {user.level}");
            
            // Kinh nghiệm (EXP)
            SetText("Lbl_ExpStatus", $"{user.exp} / {user.expNeeded} EXP");
            var expBar = _root.Q<VisualElement>("ExpBarFill");
            if (expBar != null && user.expNeeded > 0) {
                float progress = (float)user.exp / user.expNeeded * 100f;
                expBar.style.width = new Length(progress, LengthUnit.Percent);
            }

            SetText("Lbl_Coins", user.coins.ToString("N0"));
            SetText("Lbl_Rank", string.IsNullOrEmpty(user.rank) ? "Unranked" : user.rank);


            // --- KẾT NỐI NÚT BẤM ---
            _root.Q<Button>("BtnSetting")?.RegisterCallback<ClickEvent>(evt => _onNavigate?.Invoke(settingAsset));
            _root.Q<Button>("BtnAchievements")?.RegisterCallback<ClickEvent>(evt => _onNavigate?.Invoke(achievementAsset));
            _root.Q<Button>("BtnInventory")?.RegisterCallback<ClickEvent>(evt => _onNavigate?.Invoke(InventoryAsset));
        }

        private void SetText(string elementName, string value)
        {
            var label = _root.Q<Label>(elementName);
            if (label != null) label.text = value;
        }
    }
}