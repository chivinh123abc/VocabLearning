using UnityEngine;
using UnityEngine.UIElements;

namespace VocabLearning.UI
{
    public partial class VocabStudyController 
    {
        private VisualElement _settingsOverlay;

        private void CreateFloatingSettingsButton()
        {
            if (_root == null) return;

            var existingBtn = _root.Q<Button>("FloatingSettingsBtn");
            if (existingBtn != null)
            {
                existingBtn.BringToFront();
                return;
            }

            Button settingsBtn = new Button();
            settingsBtn.name = "FloatingSettingsBtn";
            settingsBtn.text = "⚙";
            
            settingsBtn.style.position = Position.Absolute;
            settingsBtn.style.top = 16;
            settingsBtn.style.right = 16;
            settingsBtn.style.width = 48;
            settingsBtn.style.height = 48;
            settingsBtn.style.borderRadius = 24;
            settingsBtn.style.backgroundColor = new StyleColor(new Color(0.12f, 0.16f, 0.24f, 0.8f));
            settingsBtn.style.color = Color.white;
            settingsBtn.style.fontSize = 24;
            settingsBtn.style.borderTopWidth = 0;
            settingsBtn.style.borderBottomWidth = 0;
            settingsBtn.style.borderLeftWidth = 0;
            settingsBtn.style.borderRightWidth = 0;
            settingsBtn.style.unityFontStyleAndWeight = FontStyle.Bold;

            settingsBtn.clicked += ShowSettingsPopup;

            _root.Add(settingsBtn);
        }

        public void ShowSettingsPopup()
        {
            if (_root == null) return;

          
            if (SettingsPopupAsset != null)
            {
                if (_settingsOverlay == null)
                {
                    SettingsPopupAsset.CloneTree(_root);
                    _settingsOverlay = _root.Q<VisualElement>("root"); 
                    if (_settingsOverlay == null) _settingsOverlay = _root.Q<VisualElement>("SettingsOverlay"); 
                    BindSettingsEvents();
                }
                
                if (_settingsOverlay != null)
                {
                    _settingsOverlay.style.display = DisplayStyle.Flex;
                    _settingsOverlay.BringToFront();
                }
                return;
            }

            Debug.LogWarning("SettingsPopupAsset is not assigned in Inspector!");
        }

        private void BindSettingsEvents()
        {
            if (_settingsOverlay == null) return;

            Slider sldMusic = _settingsOverlay.Q<Slider>("SldMusicVolume");
            if (sldMusic != null)
            {
        
                BackgroundMusic bgMusic = FindObjectOfType<BackgroundMusic>();
                if (bgMusic != null) sldMusic.value = bgMusic.volume;

                sldMusic.RegisterValueChangedCallback(evt => {
                    BackgroundMusic.SetVolume(evt.newValue);
                });
            }

            Slider sldSfx = _settingsOverlay.Q<Slider>("SldSfxVolume");
            if (sldSfx != null)
            {
                sldSfx.RegisterValueChangedCallback(evt => {
                    SoundManager.SetVolume(evt.newValue);
                });
            }

            Button btnLogout = _settingsOverlay.Q<Button>("BtnLogout");
            if (btnLogout != null)
            {
                btnLogout.clicked += () => {
                    _settingsOverlay.style.display = DisplayStyle.None;
                    if (_jsonDb != null) _jsonDb.currentUser = null; 
                    LoadScreen(AuthScreenAsset); 
                };
            }

            Button btnClose = _settingsOverlay.Q<Button>("BtnCloseSettings");
            if (btnClose != null)
            {
                btnClose.clicked += () => {
                    _settingsOverlay.style.display = DisplayStyle.None;
                };
            }
        }
    }
}
