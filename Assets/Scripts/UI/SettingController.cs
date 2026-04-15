using UnityEngine.UIElements;
using System;
using UnityEngine;

namespace VocabLearning.UI
{
    public class SettingController
    {
        private VisualElement _root;
        private Action<VisualTreeAsset> _onNavigate;
        private VocabLearning.Data.MockDatabase _db;

        public SettingController(VisualElement root, VocabLearning.Data.MockDatabase db, Action<VisualTreeAsset> onNavigate)
        {
            _root = root;
            _db = db;
            _onNavigate = onNavigate;
        }

        public void Bind(VisualTreeAsset profileAsset)
        {
            if (_db == null || _db.currentUser == null) return;
            var user = _db.currentUser;
            
            //  XỬ LÝ NÚT BACK
            var btnBack = _root.Q<VisualElement>("btnBackProFile");
            if(btnBack != null)
            {
                btnBack.RegisterCallback<ClickEvent>(evt => _onNavigate?.Invoke(profileAsset));

            }

            // Username: Cho phép sửa
            var inputUser = _root.Q<TextField>("InputUsername");
            if (inputUser != null) inputUser.value = user.username;

            var inputEmail = _root.Q<TextField>("InputEmail");
            if (inputEmail != null) 
            {
                inputEmail.value = user.email;            
              
                inputEmail.isReadOnly = true;                
                inputEmail.SetEnabled(false);          
                inputEmail.style.opacity = 0.7f;
            }

            var Lbl_level = _root.Q<Label>("Lbl_level");
            if(Lbl_level != null)
            {
                Lbl_level.text = $"Level {user.level}";
            }
            
            var progressBar = _root.Q<VisualElement>("LevelProgressBarFill");
            if (progressBar != null && user.expNeeded > 0)
            {
                float progressPercent = (float)user.exp / user.expNeeded * 100f;
                progressBar.style.width = new Length(progressPercent,LengthUnit.Percent);
            }

            //Nut save
            var btnSave = _root.Q<Button>("BtnSaveChanges");
            if (btnSave != null)
            {
                btnSave.clicked += () =>
                {
                    if (inputUser != null) user.username = inputUser.value;
                    
                    Debug.Log("Đã cập nhật Profile!");
                    _onNavigate?.Invoke(profileAsset);
                };
            }
        }
    }
}