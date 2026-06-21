using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace VocabLearning.UI
{
    public partial class VocabStudyController
    {
        private void BindQuestEvents()
        {
            BindBottomNav();

            Button tabActive = _root.Q<Button>("TabActive");
            Button tabCompleted = _root.Q<Button>("TabCompleted");

            if (tabActive != null)
            {
                tabActive.clicked += () =>
                {
                    _questCurrentTab = "Active";
                    tabActive.AddToClassList("tab-btn-active");
                    if (tabCompleted != null) tabCompleted.RemoveFromClassList("tab-btn-active");
                    RenderQuestList();
                };
            }

            if (tabCompleted != null)
            {
                tabCompleted.clicked += () =>
                {
                    _questCurrentTab = "Completed";
                    tabCompleted.AddToClassList("tab-btn-active");
                    if (tabActive != null) tabActive.RemoveFromClassList("tab-btn-active");
                    RenderQuestList();
                };
            }

            // Init state
            if (_questCurrentTab == "Active" && tabActive != null && tabCompleted != null)
            {
                tabActive.AddToClassList("tab-btn-active");
                tabCompleted.RemoveFromClassList("tab-btn-active");
            }
            else if (_questCurrentTab == "Completed" && tabActive != null && tabCompleted != null)
            {
                tabCompleted.AddToClassList("tab-btn-active");
                tabActive.RemoveFromClassList("tab-btn-active");
            }

            RenderQuestList();
        }

        private void RenderQuestList()
        {
            if (_jsonDb == null || _jsonDb.quests == null) return;

            VisualElement listContainer = _root.Q<VisualElement>("QuestListContainer");
            if (listContainer == null) return;

            listContainer.Clear();

            int activeCount = _jsonDb.quests.FindAll(q => !q.isClaimed).Count;
            int completedCount = _jsonDb.quests.FindAll(q => q.isClaimed).Count;

            Button tabActive = _root.Q<Button>("TabActive");
            Button tabCompleted = _root.Q<Button>("TabCompleted");
            if (tabActive != null) tabActive.text = $"Active ({activeCount})";
            if (tabCompleted != null) tabCompleted.text = $"Completed ({completedCount})";

            var filteredQuests = _questCurrentTab == "Active"
                ? _jsonDb.quests.FindAll(q => !q.isClaimed)
                : _jsonDb.quests.FindAll(q => q.isClaimed);

            // --- PHẦN 1: TIẾN ĐỘ TUẦN (Chỉ hiện ở tab Active) ---
            if (_questCurrentTab == "Active" && _jsonDb.currentUser.weeklyLogin != null)
            {
                var weekly = _jsonDb.currentUser.weeklyLogin;

                VisualElement weeklyCard = new VisualElement();
                weeklyCard.AddToClassList("card");
                weeklyCard.style.marginBottom = 24;
                weeklyCard.style.backgroundColor = new StyleColor(new Color(0.12f, 0.16f, 0.23f, 0.8f));
                weeklyCard.style.borderTopWidth = 2;
                weeklyCard.style.borderTopColor = new StyleColor(new Color(0.55f, 0.36f, 0.96f)); // Purple theme for weekly

                Label weeklyHeader = new Label("📅 WEEKLY LOGIN REWARD");
                weeklyHeader.AddToClassList("font-bold");
                weeklyHeader.style.color = new StyleColor(new Color(0.55f, 0.36f, 0.96f));
                weeklyHeader.style.fontSize = 14;
                weeklyHeader.style.marginBottom = 12;
                weeklyCard.Add(weeklyHeader);

                VisualElement dayContainer = new VisualElement();
                dayContainer.style.flexDirection = FlexDirection.Row;
                dayContainer.style.justifyContent = Justify.SpaceBetween;
                dayContainer.style.marginBottom = 16;

                int loginCount = weekly.loginDates.Count;
                for (int d = 1; d <= 5; d++)
                {
                    VisualElement dayCircle = new VisualElement();
                    dayCircle.style.width = 36;
                    dayCircle.style.height = 36;
                    dayCircle.style.borderTopLeftRadius = 18;
                    dayCircle.style.borderTopRightRadius = 18;
                    dayCircle.style.borderBottomLeftRadius = 18;
                    dayCircle.style.borderBottomRightRadius = 18;
                    dayCircle.style.justifyContent = Justify.Center;
                    dayCircle.style.alignItems = Align.Center;

                    if (d <= loginCount)
                    {
                        dayCircle.style.backgroundColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f)); // Green
                        Label check = new Label("✓");
                        check.style.color = Color.white;
                        check.style.unityFontStyleAndWeight = FontStyle.Bold;
                        dayCircle.Add(check);
                    }
                    else
                    {
                        dayCircle.style.backgroundColor = new StyleColor(new Color(0.2f, 0.25f, 0.33f)); // Dark
                        Label num = new Label(d.ToString());
                        num.style.color = new StyleColor(new Color(0.58f, 0.64f, 0.72f));
                        dayCircle.Add(num);
                    }
                    dayContainer.Add(dayCircle);
                }
                weeklyCard.Add(dayContainer);

                VisualElement weeklyInfo = new VisualElement();
                weeklyInfo.style.flexDirection = FlexDirection.Row;
                weeklyInfo.style.justifyContent = Justify.SpaceBetween;
                weeklyInfo.style.alignItems = Align.Center;

                VisualElement rewardInfo = new VisualElement();
                Label rewardTitle = new Label(weekly.isRewardClaimed ? "Weekly Reward Claimed!" : "Reward: 1000 Coins + 2000 EXP");
                rewardTitle.style.color = weekly.isRewardClaimed ? new StyleColor(new Color(0.06f, 0.73f, 0.51f)) : Color.white;
                rewardTitle.style.fontSize = 12;
                rewardInfo.Add(rewardTitle);

                Label progressText = new Label($"{loginCount}/5 Days");
                progressText.AddToClassList("text-muted");
                progressText.style.fontSize = 12;

                weeklyInfo.Add(rewardInfo);
                weeklyInfo.Add(progressText);
                weeklyCard.Add(weeklyInfo);

                listContainer.Add(weeklyCard);

                // --- PHẦN 2: TIÊU ĐỀ QUEST NGÀY ---
                Label dailyHeader = new Label("🌟 DAILY QUESTS");
                dailyHeader.AddToClassList("font-bold");
                dailyHeader.style.color = new StyleColor(new Color(0.58f, 0.64f, 0.72f));
                dailyHeader.style.fontSize = 12;
                dailyHeader.style.marginBottom = 12;
                listContainer.Add(dailyHeader);
            }

            foreach (var quest in filteredQuests)
            {
                VisualElement card = new VisualElement();
                card.AddToClassList("card");
                card.style.marginBottom = 16;
                card.style.borderTopColor = new StyleColor(new Color(0.2f, 0.25f, 0.33f)); // Default dark border
                card.style.borderBottomColor = new StyleColor(new Color(0.2f, 0.25f, 0.33f));
                card.style.borderLeftColor = new StyleColor(new Color(0.2f, 0.25f, 0.33f));
                card.style.borderRightColor = new StyleColor(new Color(0.2f, 0.25f, 0.33f));

                bool isReadyToClaim = quest.currentProgress >= quest.maxProgress && !quest.isClaimed;
                if (quest.isClaimed || isReadyToClaim)
                {
                    card.style.borderTopColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f)); // Green border
                    card.style.borderBottomColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f));
                    card.style.borderLeftColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f));
                    card.style.borderRightColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f));
                }

                VisualElement header = new VisualElement();
                header.style.flexDirection = FlexDirection.Row;
                header.style.justifyContent = Justify.SpaceBetween;
                header.style.alignItems = Align.FlexStart;
                header.style.marginBottom = 12;

                VisualElement textGroup = new VisualElement();
                textGroup.style.flexGrow = 1;
                textGroup.style.marginRight = 12;

                Label titleLbl = new Label(quest.title);
                titleLbl.AddToClassList("font-bold");
                titleLbl.AddToClassList("text-lg");
                titleLbl.style.color = Color.white;
                titleLbl.style.marginBottom = 4;
                textGroup.Add(titleLbl);

                Label descLbl = new Label(quest.description);
                descLbl.AddToClassList("text-muted");
                descLbl.style.whiteSpace = WhiteSpace.Normal;
                textGroup.Add(descLbl);
                header.Add(textGroup);

                if (quest.isClaimed || isReadyToClaim)
                {
                    VisualElement checkIconBox = new VisualElement();
                    checkIconBox.style.width = 32;
                    checkIconBox.style.height = 32;
                    checkIconBox.style.backgroundColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f));
                    checkIconBox.style.borderTopLeftRadius = 16;
                    checkIconBox.style.borderTopRightRadius = 16;
                    checkIconBox.style.borderBottomLeftRadius = 16;
                    checkIconBox.style.borderBottomRightRadius = 16;
                    checkIconBox.style.justifyContent = Justify.Center;
                    checkIconBox.style.alignItems = Align.Center;

                    Label checkLbl = new Label("V");
                    checkLbl.style.color = Color.white;
                    checkLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                    checkIconBox.Add(checkLbl);
                    header.Add(checkIconBox);
                }
                card.Add(header);

                // Progress Bar
                VisualElement progWrapper = new VisualElement();
                progWrapper.style.marginBottom = 12;

                VisualElement progTextRow = new VisualElement();
                progTextRow.style.flexDirection = FlexDirection.Row;
                progTextRow.style.justifyContent = Justify.SpaceBetween;
                progTextRow.style.marginBottom = 4;

                Label progTitle = new Label("Progress");
                progTitle.AddToClassList("text-muted");
                progTitle.style.fontSize = 12;
                progTextRow.Add(progTitle);

                Label progVal = new Label($"{quest.currentProgress} / {quest.maxProgress}");
                progVal.style.color = Color.white;
                progVal.style.unityFontStyleAndWeight = FontStyle.Bold;
                progVal.style.fontSize = 12;
                progTextRow.Add(progVal);
                progWrapper.Add(progTextRow);

                VisualElement progContainer = new VisualElement();
                progContainer.AddToClassList("progress-bar-container");
                progContainer.style.height = 8;
                progContainer.style.borderTopLeftRadius = 4;
                progContainer.style.borderTopRightRadius = 4;
                progContainer.style.borderBottomLeftRadius = 4;
                progContainer.style.borderBottomRightRadius = 4;

                VisualElement progFill = new VisualElement();
                float pct = quest.maxProgress > 0 ? Mathf.Clamp01((float)quest.currentProgress / quest.maxProgress) * 100f : 0;
                progFill.style.width = new Length(pct, LengthUnit.Percent);
                progFill.style.height = new Length(100, LengthUnit.Percent);
                progFill.style.backgroundColor = (quest.isClaimed || isReadyToClaim)
                    ? new StyleColor(new Color(0.06f, 0.73f, 0.51f))
                    : new StyleColor(new Color(0.23f, 0.51f, 0.96f)); // Green or Blue
                progFill.style.borderTopLeftRadius = 4;
                progFill.style.borderTopRightRadius = 4;
                progFill.style.borderBottomLeftRadius = 4;
                progFill.style.borderBottomRightRadius = 4;
                progContainer.Add(progFill);
                progWrapper.Add(progContainer);
                card.Add(progWrapper);

                // Rewards & Claim Button
                VisualElement bottomRow = new VisualElement();
                bottomRow.style.flexDirection = FlexDirection.Row;
                bottomRow.style.justifyContent = Justify.SpaceBetween;
                bottomRow.style.alignItems = Align.Center;
                bottomRow.style.borderTopWidth = 1;
                bottomRow.style.borderTopColor = new StyleColor(new Color(0.2f, 0.25f, 0.33f));
                bottomRow.style.paddingTop = 12;

                VisualElement rewardsRow = new VisualElement();
                rewardsRow.style.flexDirection = FlexDirection.Row;
                rewardsRow.style.alignItems = Align.Center;

                if (quest.rewardCoins > 0)
                {
                    Label coinReward = new Label($"O +{quest.rewardCoins}");
                    coinReward.style.color = new StyleColor(new Color(0.96f, 0.62f, 0.04f)); // #F59E0B
                    coinReward.style.unityFontStyleAndWeight = FontStyle.Bold;
                    coinReward.style.marginRight = 12;
                    rewardsRow.Add(coinReward);
                }

                if (quest.rewardExp > 0)
                {
                    Label expReward = new Label($"⚡ +{quest.rewardExp}");
                    expReward.style.color = new StyleColor(new Color(0.23f, 0.51f, 0.96f)); // #3B82F6
                    expReward.style.unityFontStyleAndWeight = FontStyle.Bold;
                    rewardsRow.Add(expReward);
                }

                bottomRow.Add(rewardsRow);

                if (isReadyToClaim)
                {
                    Button claimBtn = new Button();
                    claimBtn.text = "Claim";
                    claimBtn.AddToClassList("btn-primary");
                    claimBtn.style.backgroundColor = new StyleColor(new Color(0.96f, 0.62f, 0.04f));
                    claimBtn.style.paddingTop = 6;
                    claimBtn.style.paddingBottom = 6;
                    claimBtn.style.paddingLeft = 16;
                    claimBtn.style.paddingRight = 16;
                    claimBtn.style.marginTop = 0;
                    claimBtn.style.marginBottom = 0;
                    claimBtn.style.marginLeft = 0;
                    claimBtn.style.marginRight = 0;
                    claimBtn.style.minHeight = 30;

                    claimBtn.clicked += () =>
                    {
                        ClaimQuest(quest);
                    };

                    bottomRow.Add(claimBtn);
                }

                card.Add(bottomRow);
                listContainer.Add(card);
            }
        }

        private void ClaimQuest(VocabLearning.Data.QuestJson quest)
        {
            if (quest.isClaimed || quest.currentProgress < quest.maxProgress) return;

            // Update user balance
            if (_jsonDb != null && _jsonDb.currentUser != null)
            {
                _jsonDb.currentUser.coins += quest.rewardCoins;
                _jsonDb.currentUser.exp += quest.rewardExp;
            }

            quest.isClaimed = true;
            SaveJsonDatabase();
            UpdateAllCoinLabels();
            RenderQuestList();
        }

        private void BindAchievementEvents()
        {
            Button backBtn = _root.Q<Button>("BtnBack");
            if (backBtn != null) backBtn.clicked += () => LoadScreen(ProfileScreenAsset);

            if (_jsonDb != null && _jsonDb.achievements != null)
            {
                int unlockedCount = 0;
                int totalCount = _jsonDb.achievements.Count;

                VisualElement listContainer = _root.Q<VisualElement>("AchievementListContainer");
                if (listContainer != null)
                {
                    listContainer.Clear();
                    foreach (var ach in _jsonDb.achievements)
                    {
                        if (ach.isUnlocked) unlockedCount++;

                        VisualElement card = new VisualElement();
                        card.AddToClassList("card");
                        card.style.marginBottom = 16;
                        card.style.flexDirection = FlexDirection.Row;
                        card.style.alignItems = Align.Center;

                        if (!ach.isUnlocked)
                        {
                            card.style.opacity = 0.6f;
                        }
                        else
                        {
                            card.style.borderTopColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f, 1f)); // #10B981
                            card.style.borderBottomColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f, 1f));
                            card.style.borderLeftColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f, 1f));
                            card.style.borderRightColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f, 1f));
                            card.style.borderTopWidth = 1;
                            card.style.borderBottomWidth = 1;
                            card.style.borderLeftWidth = 1;
                            card.style.borderRightWidth = 1;
                        }

                        // Icon box
                        VisualElement iconBox = new VisualElement();
                        iconBox.style.width = 60;
                        iconBox.style.height = 60;
                        iconBox.style.borderTopLeftRadius = 16;
                        iconBox.style.borderTopRightRadius = 16;
                        iconBox.style.borderBottomLeftRadius = 16;
                        iconBox.style.borderBottomRightRadius = 16;
                        iconBox.style.justifyContent = Justify.Center;
                        iconBox.style.alignItems = Align.Center;
                        iconBox.style.marginRight = 16;

                        if (ach.isUnlocked)
                            iconBox.style.backgroundColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f, 0.2f));
                        else
                            iconBox.style.backgroundColor = new StyleColor(new Color(0.2f, 0.25f, 0.33f, 1f)); // #334155

                        Label iconLbl = new Label(ach.icon);
                        iconLbl.style.fontSize = 28;
                        iconBox.Add(iconLbl);
                        card.Add(iconBox);

                        // Text Group
                        VisualElement textGroup = new VisualElement();
                        textGroup.style.flexGrow = 1;

                        Label titleLbl = new Label(ach.title);
                        titleLbl.AddToClassList("font-bold");
                        titleLbl.AddToClassList("text-lg");
                        titleLbl.style.color = Color.white;
                        titleLbl.style.marginBottom = 4;
                        textGroup.Add(titleLbl);

                        Label descLbl = new Label(ach.description);
                        descLbl.AddToClassList("text-muted");
                        descLbl.style.whiteSpace = WhiteSpace.Normal;
                        descLbl.style.fontSize = 13;
                        textGroup.Add(descLbl);

                        if (ach.isUnlocked)
                        {
                            Label unlockedDateLbl = new Label($"Unlocked: {ach.unlockDate}");
                            unlockedDateLbl.style.color = new StyleColor(new Color(0.06f, 0.73f, 0.51f, 1f));
                            unlockedDateLbl.style.fontSize = 11;
                            unlockedDateLbl.style.marginTop = 6;
                            unlockedDateLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                            textGroup.Add(unlockedDateLbl);
                        }
                        else
                        {
                            // Progress bar
                            VisualElement progressRow = new VisualElement();
                            progressRow.style.flexDirection = FlexDirection.Row;
                            progressRow.style.alignItems = Align.Center;
                            progressRow.style.marginTop = 8;

                            VisualElement progContainer = new VisualElement();
                            progContainer.AddToClassList("progress-bar-container");
                            progContainer.style.flexGrow = 1;
                            progContainer.style.height = 6;
                            progContainer.style.marginRight = 8;
                            progContainer.style.borderTopLeftRadius = 3;
                            progContainer.style.borderTopRightRadius = 3;
                            progContainer.style.borderBottomLeftRadius = 3;
                            progContainer.style.borderBottomRightRadius = 3;

                            VisualElement progFill = new VisualElement();
                            float pct = ach.maxProgress > 0 ? ((float)ach.currentProgress / ach.maxProgress) * 100f : 0;
                            progFill.style.width = new Length(pct, LengthUnit.Percent);
                            progFill.style.height = new Length(100, LengthUnit.Percent);
                            progFill.style.backgroundColor = new StyleColor(new Color(0.58f, 0.64f, 0.72f, 1f)); // #94A3B8
                            progFill.style.borderTopLeftRadius = 3;
                            progFill.style.borderTopRightRadius = 3;
                            progFill.style.borderBottomLeftRadius = 3;
                            progFill.style.borderBottomRightRadius = 3;
                            progContainer.Add(progFill);
                            progressRow.Add(progContainer);

                            Label progText = new Label($"{ach.currentProgress}/{ach.maxProgress}");
                            progText.AddToClassList("text-muted");
                            progText.style.fontSize = 11;
                            progressRow.Add(progText);

                            textGroup.Add(progressRow);
                        }

                        card.Add(textGroup);
                        listContainer.Add(card);
                    }
                }

                // Update Completion Stats
                Label lblCompletionCount = _root.Q<Label>("LblCompletionCount");
                if (lblCompletionCount != null)
                {
                    lblCompletionCount.text = $"{unlockedCount}/{totalCount}";
                }

                VisualElement progressFill = _root.Q<VisualElement>("ProgressBarCompletionFill");
                if (progressFill != null)
                {
                    if (totalCount > 0)
                    {
                        float fillPct = ((float)unlockedCount / totalCount) * 100f;
                        progressFill.style.width = new Length(fillPct, LengthUnit.Percent);
                    }
                    else
                    {
                        progressFill.style.width = new Length(0, LengthUnit.Percent);
                    }
                }
            }
        }
    }
}
