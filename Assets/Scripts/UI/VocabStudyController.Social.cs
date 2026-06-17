using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace VocabLearning.UI
{
    public partial class VocabStudyController
    {
        private void BindFriendEvents()
        {
            Button backBtn = _root.Q<Button>("BtnBack");
            if (backBtn != null) backBtn.clicked += () => LoadScreen(HomeScreenAsset);

            // -- Thanh Tìm kiếm Bạn Bè --
            TextField searchBox = _root.Q<TextField>(className: "search-input");
            if (searchBox != null)
            {
                SetupPlaceholder(searchBox, "Search friends...");
            }
        }

        private void BindRankingEvents()
        {
            BindBottomNav();
            RenderRankingList();
        }

        private void RenderRankingList()
        {
            if (_jsonDb == null || _jsonDb.currentUser == null) return;

            // 1. Combine
            System.Collections.Generic.List<VocabLearning.Data.UserJson> allUsers = new System.Collections.Generic.List<VocabLearning.Data.UserJson>();
            allUsers.Add(_jsonDb.currentUser);
            if (_jsonDb.leaderboardUsers != null)
            {
                allUsers.AddRange(_jsonDb.leaderboardUsers);
            }

            // 2. Sort by Rank Points descending
            allUsers.Sort((a, b) => b.rankPoints.CompareTo(a.rankPoints));

            // 3. Fill Podium (Top 1, 2, 3)
            if (allUsers.Count > 0)
            {
                var top1 = allUsers[0];
                var tier1 = GetRankTier(top1.rankPoints);
                Label top1Name = _root.Q<Label>("Podium1Name");
                if (top1Name != null)
                {
                    top1Name.text = $"{tier1.icon} {(top1.id == _jsonDb.currentUser.id ? "You" : (string.IsNullOrEmpty(top1.displayName) ? top1.username : top1.displayName))}";
                    top1Name.style.color = new StyleColor(tier1.color);
                }
                Label top1Score = _root.Q<Label>("Podium1Score");
                if (top1Score != null) top1Score.text = top1.rankPoints.ToString();
                Label top1Avatar = _root.Q<Label>("Podium1Avatar");
                if (top1Avatar != null) top1Avatar.text = GetUserAvatar(top1.id);
            }
            if (allUsers.Count > 1)
            {
                var top2 = allUsers[1];
                var tier2 = GetRankTier(top2.rankPoints);
                Label top2Name = _root.Q<Label>("Podium2Name");
                if (top2Name != null)
                {
                    top2Name.text = $"{tier2.icon} {(top2.id == _jsonDb.currentUser.id ? "You" : (string.IsNullOrEmpty(top2.displayName) ? top2.username : top2.displayName))}";
                    top2Name.style.color = new StyleColor(tier2.color);
                }
                Label top2Score = _root.Q<Label>("Podium2Score");
                if (top2Score != null) top2Score.text = top2.rankPoints.ToString();
                Label top2Avatar = _root.Q<Label>("Podium2Avatar");
                if (top2Avatar != null) top2Avatar.text = GetUserAvatar(top2.id);
            }
            if (allUsers.Count > 2)
            {
                var top3 = allUsers[2];
                var tier3 = GetRankTier(top3.rankPoints);
                Label top3Name = _root.Q<Label>("Podium3Name");
                if (top3Name != null)
                {
                    top3Name.text = $"{tier3.icon} {(top3.id == _jsonDb.currentUser.id ? "You" : (string.IsNullOrEmpty(top3.displayName) ? top3.username : top3.displayName))}";
                    top3Name.style.color = new StyleColor(tier3.color);
                }
                Label top3Score = _root.Q<Label>("Podium3Score");
                if (top3Score != null) top3Score.text = top3.rankPoints.ToString();
                Label top3Avatar = _root.Q<Label>("Podium3Avatar");
                if (top3Avatar != null) top3Avatar.text = GetUserAvatar(top3.id);
            }

            // 4. Fill MyRankContainer
            int myRankIndex = allUsers.FindIndex(u => u.id == _jsonDb.currentUser.id);
            if (myRankIndex >= 0)
            {
                var myTier = GetRankTier(_jsonDb.currentUser.rankPoints);

                Label myRankVal = _root.Q<Label>("MyRankValue");
                if (myRankVal != null) myRankVal.text = (myRankIndex + 1).ToString();

                Label myRankLvl = _root.Q<Label>("MyRankLevel");
                if (myRankLvl != null)
                {
                    myRankLvl.text = $"{myTier.icon} {myTier.name} • Level {CalculateLevel(_jsonDb.currentUser.exp)}";
                    myRankLvl.style.color = new StyleColor(myTier.color);
                }

                Label myRankScore = _root.Q<Label>("MyRankScore");
                if (myRankScore != null) myRankScore.text = _jsonDb.currentUser.rankPoints.ToString();

                Label myRankAvatar = _root.Q<Label>("MyRankAvatar");
                if (myRankAvatar != null) myRankAvatar.text = GetUserAvatar(_jsonDb.currentUser.id);
            }

            // 5. Fill Other Rankings (Rank 4 onwards)
            VisualElement listContainer = _root.Q<VisualElement>("OtherRankingsContainer");
            if (listContainer != null)
            {
                listContainer.Clear();
                for (int i = 3; i < allUsers.Count; i++)
                {
                    var u = allUsers[i];

                    VisualElement card = new VisualElement();
                    card.AddToClassList("card");
                    card.style.flexDirection = FlexDirection.Row;
                    card.style.alignItems = Align.Center;
                    card.style.justifyContent = Justify.SpaceBetween;
                    card.style.marginBottom = 8;

                    if (u.id == _jsonDb.currentUser.id)
                    {
                        card.style.borderTopColor = new StyleColor(new Color(0.23f, 0.51f, 0.96f)); // Blue highlight
                        card.style.borderBottomColor = new StyleColor(new Color(0.23f, 0.51f, 0.96f));
                        card.style.borderLeftColor = new StyleColor(new Color(0.23f, 0.51f, 0.96f));
                        card.style.borderRightColor = new StyleColor(new Color(0.23f, 0.51f, 0.96f));
                        card.style.backgroundColor = new StyleColor(new Color(0.23f, 0.51f, 0.96f, 0.1f));
                    }

                    // Left Side Group
                    VisualElement leftGroup = new VisualElement();
                    leftGroup.style.flexDirection = FlexDirection.Row;
                    leftGroup.style.alignItems = Align.Center;

                    VisualElement rankBox = new VisualElement();
                    rankBox.style.width = 40;
                    rankBox.style.height = 40;
                    rankBox.style.backgroundColor = new StyleColor(new Color(0.12f, 0.16f, 0.23f));
                    rankBox.style.borderTopLeftRadius = 8;
                    rankBox.style.borderTopRightRadius = 8;
                    rankBox.style.borderBottomLeftRadius = 8;
                    rankBox.style.borderBottomRightRadius = 8;
                    rankBox.style.justifyContent = Justify.Center;
                    rankBox.style.alignItems = Align.Center;
                    rankBox.style.marginRight = 12;

                    Label rankLbl = new Label((i + 1).ToString());
                    rankLbl.AddToClassList("font-bold");
                    rankLbl.style.color = new StyleColor(new Color(0.58f, 0.64f, 0.72f));
                    rankBox.Add(rankLbl);

                    VisualElement avatarBox = new VisualElement();
                    avatarBox.style.width = 48;
                    avatarBox.style.height = 48;
                    avatarBox.style.backgroundColor = new StyleColor(new Color(0.39f, 0.45f, 0.55f));
                    if (u.id == _jsonDb.currentUser.id) avatarBox.style.backgroundColor = new StyleColor(new Color(0.23f, 0.51f, 0.96f));
                    avatarBox.style.borderTopLeftRadius = 12;
                    avatarBox.style.borderTopRightRadius = 12;
                    avatarBox.style.borderBottomLeftRadius = 12;
                    avatarBox.style.borderBottomRightRadius = 12;
                    avatarBox.style.marginRight = 12;
                    avatarBox.style.justifyContent = Justify.Center;
                    avatarBox.style.alignItems = Align.Center;

                    Label avatarLbl = new Label(GetUserAvatar(u.id));
                    avatarLbl.style.fontSize = 24;
                    avatarBox.Add(avatarLbl);

                    VisualElement infoBox = new VisualElement();

                    var tier = GetRankTier(u.rankPoints);

                    Label nameLbl = new Label(u.id == _jsonDb.currentUser.id ? "You" : (string.IsNullOrEmpty(u.displayName) ? u.username : u.displayName));
                    nameLbl.AddToClassList("font-bold");
                    nameLbl.style.color = Color.white;
                    nameLbl.style.fontSize = 16;
                    nameLbl.style.marginBottom = 2;
                    infoBox.Add(nameLbl);

                    Label levelLbl = new Label($"{tier.icon} {tier.name} • Level {CalculateLevel(u.exp)}");
                    levelLbl.AddToClassList("font-bold");
                    levelLbl.style.fontSize = 12;
                    levelLbl.style.color = new StyleColor(tier.color);

                    infoBox.Add(levelLbl);

                    leftGroup.Add(rankBox);
                    leftGroup.Add(avatarBox);
                    leftGroup.Add(infoBox);

                    // Right Side Group
                    VisualElement rightGroup = new VisualElement();
                    rightGroup.style.alignItems = Align.FlexEnd;

                    Label scoreLbl = new Label(u.rankPoints.ToString());
                    scoreLbl.AddToClassList("font-bold");
                    scoreLbl.style.color = Color.white;
                    scoreLbl.style.fontSize = 18;
                    rightGroup.Add(scoreLbl);

                    Label ptsLbl = new Label("Rank Points");
                    ptsLbl.AddToClassList("text-muted");
                    ptsLbl.style.fontSize = 12;
                    rightGroup.Add(ptsLbl);

                    card.Add(leftGroup);
                    card.Add(rightGroup);

                    listContainer.Add(card);
                }
            }
        }

        private void BindProfileEvents()
        {
            BindBottomNav();

            Button btnAchievements = _root.Q<Button>("BtnAchievements");
            if (btnAchievements != null) btnAchievements.clicked += () => LoadScreen(AchievementScreenAsset);

            Button btnInventory = _root.Q<Button>("BtnInventory");
            if (btnInventory != null) btnInventory.clicked += () => LoadScreen(InventoryScreenAsset);

            Button btnSettings = _root.Q<Button>("SettingButton");
            if (btnSettings != null) btnSettings.clicked += ShowSettingsPopup;

            // BIND USER DATA
            if (_jsonDb != null && _jsonDb.currentUser != null)
            {
                Label lblName = _root.Q<Label>("LblProfileName");
                if (lblName != null) lblName.text = string.IsNullOrEmpty(_jsonDb.currentUser.displayName) ? _jsonDb.currentUser.username : _jsonDb.currentUser.displayName;

                Button btnEditName = _root.Q<Button>("BtnEditProfileName");
                VisualElement editContainer = _root.Q<VisualElement>("EditProfileNameContainer");
                TextField inputName = _root.Q<TextField>("InputProfileName");
                Button btnSaveName = _root.Q<Button>("BtnSaveProfileName");
                Button btnCancelName = _root.Q<Button>("BtnCancelProfileName");

                if (btnEditName != null && editContainer != null && inputName != null && btnSaveName != null && btnCancelName != null)
                {
                    btnEditName.clicked += () =>
                    {
                        if (btnEditName.parent != null) btnEditName.parent.style.display = DisplayStyle.None;
                        editContainer.style.display = DisplayStyle.Flex;
                        inputName.value = string.IsNullOrEmpty(_jsonDb.currentUser.displayName) ? _jsonDb.currentUser.username : _jsonDb.currentUser.displayName;
                    };

                    btnCancelName.clicked += () =>
                    {
                        if (btnEditName.parent != null) btnEditName.parent.style.display = DisplayStyle.Flex;
                        editContainer.style.display = DisplayStyle.None;
                    };

                    btnSaveName.clicked += () =>
                    {
                        string newName = inputName.value.Trim();
                        if (string.IsNullOrEmpty(newName))
                        {
                            Debug.LogWarning("Tên không được để trống!");
                            return;
                        }
                        if (newName.Length < 3 || newName.Length > 20)
                        {
                            Debug.LogWarning("Tên phải dài từ 3 đến 20 ký tự!");
                            return;
                        }

                        btnSaveName.SetEnabled(false);
                        VocabLearning.Network.NetworkClient.Instance.ChangeUsername(newName, (success, msg, res) =>
                        {
                            btnSaveName.SetEnabled(true);
                            if (success && res != null && res.success)
                            {
                                Debug.Log($"[Change Username] Đổi tên thành công: {res.displayName}");
                                _jsonDb.currentUser.displayName = res.displayName;
                                lblName.text = res.displayName;
                                SaveJsonDatabase(); // Lưu thay đổi offline
                                
                                if (btnEditName.parent != null) btnEditName.parent.style.display = DisplayStyle.Flex;
                                editContainer.style.display = DisplayStyle.None;
                            }
                            else
                            {
                                Debug.LogError($"[Change Username] Thất bại khi đổi tên: {msg}");
                            }
                        });
                    };
                }

                int currentLevel = CalculateLevel(_jsonDb.currentUser.exp); // Cập nhật đúng Level thực tế
                Label lblLevel = _root.Q<Label>("LblProfileLevel");
                if (lblLevel != null) lblLevel.text = $"Level {currentLevel}";

                int lvl, curLevelExp, nextLevelExpNeeded;
                GetExpDetails(_jsonDb.currentUser.exp, out lvl, out curLevelExp, out nextLevelExpNeeded);
                float expPercent = (curLevelExp / (float)nextLevelExpNeeded) * 100f;

                Label lblExp = _root.Q<Label>("LblProfileExp");
                if (lblExp != null) lblExp.text = $"{curLevelExp} / {nextLevelExpNeeded} EXP";

                VisualElement profileExpFill = _root.Q<VisualElement>("ProfileExpFill");
                if (profileExpFill != null) profileExpFill.style.width = Length.Percent(expPercent);


                Label lblCoins = _root.Q<Label>("LblProfileCoins");
                if (lblCoins != null) lblCoins.text = _jsonDb.currentUser.coins.ToString();

                // 1. Cập nhật Rank dựa trên điểm Rank thực tế của user
                var rankTier = GetRankTier(_jsonDb.currentUser.rankPoints);
                Label lblRank = _root.Q<Label>("LblProfileRank");
                if (lblRank != null) lblRank.text = rankTier.name;

                // 2. Cập nhật các chỉ số thống kê thực tế từ DB
                Label lblTotalGames = _root.Q<Label>("LblProfileTotalGames");
                if (lblTotalGames != null) lblTotalGames.text = _jsonDb.currentUser.totalGames.ToString();

                Label lblWins = _root.Q<Label>("LblProfileWins");
                if (lblWins != null) lblWins.text = _jsonDb.currentUser.wins.ToString();

                int learnedWordsCount = 0;
                if (_jsonDb.currentUser.wordProgress != null)
                {
                    learnedWordsCount = _jsonDb.currentUser.wordProgress.FindAll(w => w.status > 0).Count;
                }
                Label lblWordsLearned = _root.Q<Label>("LblProfileWordsLearned");
                if (lblWordsLearned != null) lblWordsLearned.text = learnedWordsCount.ToString();

                int unlockedAchievementsCount = 0;
                if (_jsonDb.achievements != null)
                {
                    unlockedAchievementsCount = _jsonDb.achievements.FindAll(a => a.isUnlocked).Count;
                }
                Label lblAchievements = _root.Q<Label>("LblProfileAchievements");
                if (lblAchievements != null) lblAchievements.text = unlockedAchievementsCount.ToString();

                if (_jsonDb.inventory != null)
                {
                    var equippedAvatar = _jsonDb.inventory.Find(i => i.equipType == "Avatar" && i.isEquipped);
                    var equippedBorder = _jsonDb.inventory.Find(i => i.equipType == "Border" && i.isEquipped);
                    var equippedEffect = _jsonDb.inventory.Find(i => i.equipType == "Effect" && i.isEquipped);

                    Label avatarIcon = _root.Q<Label>("ProfileAvatarIcon");
                    if (avatarIcon != null && equippedAvatar != null)
                    {
                        avatarIcon.text = equippedAvatar.icon;
                    }

                    Label effectIcon = _root.Q<Label>("ProfileAvatarEffect");
                    if (effectIcon != null)
                    {
                        effectIcon.text = equippedEffect != null ? equippedEffect.icon : "";
                        if (equippedEffect != null) AnimateAura(effectIcon);
                    }

                    VisualElement borderContainer = _root.Q<VisualElement>("ProfileBorderContainer");
                    if (borderContainer != null && equippedBorder != null)
                    {
                        Color borderColor = Color.white;
                        if (equippedBorder.rarity == "Common") borderColor = new Color(0.23f, 0.51f, 0.96f);
                        else if (equippedBorder.rarity == "Rare") borderColor = new Color(0.96f, 0.62f, 0.04f);
                        else if (equippedBorder.rarity == "Epic") borderColor = new Color(0.55f, 0.36f, 0.96f);
                        else if (equippedBorder.rarity == "Legendary") borderColor = new Color(0.93f, 0.26f, 0.26f);

                        borderContainer.style.borderTopColor = new StyleColor(borderColor);
                        borderContainer.style.borderBottomColor = new StyleColor(borderColor);
                        borderContainer.style.borderLeftColor = new StyleColor(borderColor);
                        borderContainer.style.borderRightColor = new StyleColor(borderColor);
                    }
                }
            }
        }

        private void BindBottomNav()
        {
            Button navHome = _root.Q<Button>("NavHome");
            if (navHome != null) navHome.clicked += () => LoadScreen(HomeScreenAsset);

            Button navQuest = _root.Q<Button>("NavQuest");
            if (navQuest != null) navQuest.clicked += () => LoadScreen(QuestScreenAsset);

            Button navShop = _root.Q<Button>("NavShop");
            if (navShop != null) navShop.clicked += () => LoadScreen(ShopScreenAsset);

            Button navRank = _root.Q<Button>("NavRank");
            if (navRank != null) navRank.clicked += () => LoadScreen(RankingScreenAsset);

            Button navProfile = _root.Q<Button>("NavProfile");
            if (navProfile != null) navProfile.clicked += () => LoadScreen(ProfileScreenAsset);
        }

        private void AnimateAura(VisualElement target)
        {
            if (target == null || target.ClassListContains("is-animated")) return;
            target.AddToClassList("is-animated");

            float scale = 1.0f;
            float step = 0.015f;
            target.schedule.Execute(() =>
            {
                scale += step;
                if (scale >= 1.2f) step = -0.015f;
                if (scale <= 0.9f) step = 0.015f;

                target.style.scale = new StyleScale(new Vector2(scale, scale));
                target.style.opacity = new StyleFloat((scale - 0.9f) * 2f + 0.4f); // 0.4 -> 1.0
            }).Every(30);
        }

        private (string name, string icon, Color color) GetRankTier(int exp)
        {
            if (exp >= 20000) return ("Siêu Cấp", "👑", new Color(0.93f, 0.26f, 0.26f)); // Red
            if (exp >= 10000) return ("Kim Cương", "💎", new Color(0.12f, 0.8f, 0.98f)); // Cyan
            if (exp >= 5000) return ("Bạch Kim", "💠", new Color(0.6f, 0.4f, 0.8f)); // Purple
            if (exp >= 2500) return ("Vàng", "🌟", new Color(1.0f, 0.84f, 0.0f)); // Gold
            if (exp >= 1000) return ("Bạc", "🔹", new Color(0.75f, 0.75f, 0.75f)); // Silver/Steel
            return ("Đồng", "🔸", new Color(0.8f, 0.5f, 0.2f)); // Bronze/Orange
        }

        private string GetUserAvatar(string userId)
        {
            if (_jsonDb != null && _jsonDb.currentUser != null && userId == _jsonDb.currentUser.id)
            {
                if (_jsonDb.inventory != null)
                {
                    var equippedAvatar = _jsonDb.inventory.Find(i => i.equipType == "Avatar" && i.isEquipped);
                    if (equippedAvatar != null) return equippedAvatar.icon;
                }
                return "👤";
            }

            // Check if the user is in leaderboardUsers and has an equipped avatar
            if (_jsonDb != null && _jsonDb.leaderboardUsers != null)
            {
                var targetUser = _jsonDb.leaderboardUsers.Find(u => u.id == userId);
                if (targetUser != null && targetUser.inventory != null)
                {
                    var equippedAvatar = targetUser.inventory.Find(i => i.equipType == "Avatar" && i.isEquipped);
                    if (equippedAvatar != null) return equippedAvatar.icon;
                }
            }

            // Check if the user is the battle enemy and has an equipped avatar
            if (_battleEnemyData != null && _battleEnemyData.id == userId && _battleEnemyData.inventory != null)
            {
                var equippedAvatar = _battleEnemyData.inventory.Find(i => i.equipType == "Avatar" && i.isEquipped);
                if (equippedAvatar != null) return equippedAvatar.icon;
            }

            // Fallback for AI bots - deterministic random based on id string
            string[] botAvatars = { "🐼", "🦊", "🐯", "🐰", "🐻", "🦖", "🐧", "🦉", "🦄" };
            int hash = 0;
            if (userId != null)
            {
                foreach (char c in userId) hash += c;
            }
            return botAvatars[hash % botAvatars.Length];
        }

        private void ShowConfirmationDialog(string title, string message, System.Action onConfirm, System.Action onCancel)
        {
            // 1. Overlay (Màn phủ mờ)
            VisualElement overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.width = Length.Percent(100);
            overlay.style.height = Length.Percent(100);
            overlay.style.backgroundColor = new Color(0, 0, 0, 0.7f);
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;

            // 2. Dialog Box
            VisualElement dialog = new VisualElement();
            dialog.style.width = 320;
            dialog.style.backgroundColor = new Color(0.07f, 0.11f, 0.19f); // Dark background
            dialog.style.borderTopLeftRadius = 16;
            dialog.style.borderTopRightRadius = 16;
            dialog.style.borderBottomLeftRadius = 16;
            dialog.style.borderBottomRightRadius = 16;
            dialog.style.paddingTop = 24;
            dialog.style.paddingBottom = 24;
            dialog.style.paddingLeft = 24;
            dialog.style.paddingRight = 24;
            dialog.style.borderTopWidth = 1;
            dialog.style.borderBottomWidth = 1;
            dialog.style.borderLeftWidth = 1;
            dialog.style.borderRightWidth = 1;
            dialog.style.borderTopColor = new Color(1, 1, 1, 0.1f);
            dialog.style.borderBottomColor = new Color(1, 1, 1, 0.1f);
            dialog.style.borderLeftColor = new Color(1, 1, 1, 0.1f);
            dialog.style.borderRightColor = new Color(1, 1, 1, 0.1f);

            // 3. Content
            Label titleLbl = new Label(title);
            titleLbl.style.fontSize = 20;
            titleLbl.AddToClassList("font-bold");
            titleLbl.style.color = Color.white;
            titleLbl.style.marginBottom = 12;
            titleLbl.style.whiteSpace = WhiteSpace.Normal;
            dialog.Add(titleLbl);

            Label msgLbl = new Label(message);
            msgLbl.style.fontSize = 14;
            msgLbl.style.color = new Color(0.58f, 0.64f, 0.72f);
            msgLbl.style.marginBottom = 24;
            msgLbl.style.whiteSpace = WhiteSpace.Normal;
            dialog.Add(msgLbl);

            // 4. Buttons Container
            VisualElement btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.justifyContent = Justify.SpaceBetween;

            Button btnCancel = new Button();
            btnCancel.text = "QUAY LẠI";
            btnCancel.AddToClassList("btn-secondary");
            btnCancel.style.flexGrow = 1;
            btnCancel.style.marginRight = 6;
            btnCancel.clicked += () =>
            {
                _root.Remove(overlay);
                onCancel?.Invoke();
            };

            Button btnConfirm = new Button();
            btnConfirm.text = "ĐỒNG Ý";
            btnConfirm.AddToClassList("btn-primary");
            btnConfirm.style.flexGrow = 1;
            btnConfirm.style.marginLeft = 6;
            btnConfirm.clicked += () =>
            {
                _root.Remove(overlay);
                onConfirm?.Invoke();
            };

            btnRow.Add(btnCancel);
            btnRow.Add(btnConfirm);
            dialog.Add(btnRow);

            overlay.Add(dialog);
            _root.Add(overlay);
        }
    }
}
