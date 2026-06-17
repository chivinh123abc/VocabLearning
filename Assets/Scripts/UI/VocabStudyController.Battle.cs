using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

namespace VocabLearning.UI
{
    public partial class VocabStudyController
    {
        private string _pvpRoomId;
        private VisualElement _matchmakingOverlay;
        private Coroutine _matchmakingCoroutine;
        private Coroutine _pvpGameplayCoroutine;
        private Coroutine _nextQuestionCoroutine;

        private void BindBattleEvents()
        {
            Button backBtn = _root.Q<Button>("BtnBack");
            if (backBtn != null) backBtn.clicked += () => LoadScreen(HomeScreenAsset);

            if (_jsonDb != null && _jsonDb.currentUser != null)
            {
                var curUser = _jsonDb.currentUser;
                var tier = GetRankTier(curUser.rankPoints);

                Label lblName = _root.Q<Label>("BattleName");
                if (lblName != null) lblName.text = string.IsNullOrEmpty(curUser.displayName) ? curUser.username : curUser.displayName;

                Label lblRank = _root.Q<Label>("BattleRank");
                if (lblRank != null)
                {
                    lblRank.text = $"{tier.icon} {tier.name}";
                    lblRank.style.color = new StyleColor(tier.color);
                }

                Label lblPoints = _root.Q<Label>("BattlePoints");
                if (lblPoints != null) lblPoints.text = curUser.rankPoints.ToString();

                Label lblWins = _root.Q<Label>("BattleWins");
                if (lblWins != null) lblWins.text = curUser.wins.ToString();

                Label lblGames = _root.Q<Label>("BattleGames");
                if (lblGames != null) lblGames.text = curUser.totalGames.ToString();

                Label lblWinRate = _root.Q<Label>("BattleWinRate");
                if (lblWinRate != null)
                {
                    float winrate = curUser.totalGames > 0 ? (float)curUser.wins / curUser.totalGames * 100f : 0f;
                    lblWinRate.text = $"{Mathf.RoundToInt(winrate)}%";
                }

                Label lblAvatar = _root.Q<Label>("BattleAvatar");
                if (lblAvatar != null) lblAvatar.text = GetUserAvatar(curUser.id);
            }

            Button btnRanked = _root.Q<Button>("BtnRankedMode");
            if (btnRanked != null) btnRanked.clicked += () => StartBattle(true);

            Button btnCasual = _root.Q<Button>("BtnCasualMode");
            if (btnCasual != null) btnCasual.clicked += () => StartBattle(false);

            // -- Lịch sử trận đấu --
            Button btnViewHistory = _root.Q<Button>("BtnViewHistory");
            if (btnViewHistory != null) btnViewHistory.clicked += () => ShowBattleHistoryOverlay();

            Button btnCloseHistory = _root.Q<Button>("BtnCloseHistory");
            if (btnCloseHistory != null) btnCloseHistory.clicked += () =>
            {
                var overlay = _root.Q<VisualElement>("BattleHistoryOverlay");
                if (overlay != null) overlay.style.display = DisplayStyle.None;
            };

            Button btnCloseDetail = _root.Q<Button>("BtnCloseDetail");
            if (btnCloseDetail != null) btnCloseDetail.clicked += () =>
            {
                var detail = _root.Q<VisualElement>("HistoryDetailOverlay");
                if (detail != null) detail.style.display = DisplayStyle.None;
            };
        }

        private void ShowBattleHistoryOverlay()
        {
            var overlay = _root.Q<VisualElement>("BattleHistoryOverlay");
            if (overlay == null) return;
            overlay.style.display = DisplayStyle.Flex;

            var container = _root.Q<VisualElement>("HistoryListContainer");
            if (container == null) return;
            container.Clear();

            var history = _jsonDb?.currentUser?.battleHistory;
            if (history == null || history.Count == 0)
            {
                Label empty = new Label("Chưa có trận đấu nào trong lịch sử.");
                empty.style.color = new StyleColor(new Color(0.6f, 0.64f, 0.71f));
                empty.style.marginTop = 24;
                empty.style.unityTextAlign = TextAnchor.MiddleCenter;
                container.Add(empty);
                return;
            }

            foreach (var record in history)
            {
                VisualElement card = new VisualElement();
                card.AddToClassList("card");
                card.style.flexDirection = FlexDirection.Row;
                card.style.alignItems = Align.Center;
                card.style.marginBottom = 12;
                card.style.paddingLeft = 16;
                card.style.paddingRight = 16;
                card.style.paddingTop = 12;
                card.style.paddingBottom = 12;

                // Badge WIN / LOSE
                VisualElement badge = new VisualElement();
                badge.style.width = 52;
                badge.style.height = 52;
                badge.style.borderTopLeftRadius = 12;
                badge.style.borderTopRightRadius = 12;
                badge.style.borderBottomLeftRadius = 12;
                badge.style.borderBottomRightRadius = 12;
                badge.style.justifyContent = Justify.Center;
                badge.style.alignItems = Align.Center;
                badge.style.marginRight = 16;
                badge.style.backgroundColor = record.isWin
                    ? new StyleColor(new Color(0.06f, 0.44f, 0.31f))
                    : new StyleColor(new Color(0.56f, 0.16f, 0.16f));

                Label badgeLbl = new Label(record.isWin ? "W" : "L");
                badgeLbl.style.color = Color.white;
                badgeLbl.style.fontSize = 22;
                badgeLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                badge.Add(badgeLbl);
                card.Add(badge);

                // Info
                VisualElement info = new VisualElement();
                info.style.flexGrow = 1;

                Label nameLbl = new Label($"vs {record.opponentName}");
                nameLbl.style.color = Color.white;
                nameLbl.style.fontSize = 15;
                nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                info.Add(nameLbl);

                Label modeLbl = new Label($"{(record.isRanked ? "Ranked" : "Casual")}  •  {record.correctCount}/{record.totalRounds} đúng  •  {record.date}");
                modeLbl.style.color = new StyleColor(new Color(0.6f, 0.64f, 0.71f));
                modeLbl.style.fontSize = 11;
                modeLbl.style.whiteSpace = WhiteSpace.Normal;
                info.Add(modeLbl);

                card.Add(info);

                // Nút xem chi tiết
                var capturedRecord = record;
                Label arrowLbl = new Label("→");
                arrowLbl.style.color = new StyleColor(new Color(0.38f, 0.65f, 0.98f));
                arrowLbl.style.fontSize = 20;
                card.Add(arrowLbl);

                card.RegisterCallback<ClickEvent>(_ => ShowBattleHistoryDetail(capturedRecord));
                container.Add(card);
            }
        }

        private void ShowBattleHistoryDetail(VocabLearning.Data.BattleHistoryRecord record)
        {
            var detailOverlay = _root.Q<VisualElement>("HistoryDetailOverlay");
            if (detailOverlay == null) return;
            detailOverlay.style.display = DisplayStyle.Flex;

            Label titleLbl = detailOverlay.Q<Label>("DetailTitle");
            if (titleLbl != null) titleLbl.text = $"vs {record.opponentName} — {(record.isWin ? "Victory" : "Defeat")}";

            Label statsLbl = detailOverlay.Q<Label>("DetailStats");
            if (statsLbl != null) statsLbl.text = $"{record.correctCount} / {record.totalRounds} đúng";

            VisualElement detailList = detailOverlay.Q<VisualElement>("DetailList");
            if (detailList == null) return;
            detailList.Clear();

            foreach (var round in record.rounds)
            {
                VisualElement row = CreateBattleRoundRow(round);
                detailList.Add(row);
            }
        }

        private VisualElement CreateBattleRoundRow(VocabLearning.Data.BattleRoundRecord round)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row; // Changed to Row to fit image
            row.style.marginBottom = 12;
            row.style.paddingLeft = 16;
            row.style.paddingRight = 16;
            row.style.paddingTop = 14;
            row.style.paddingBottom = 14;
            row.style.borderTopLeftRadius = 12;
            row.style.borderTopRightRadius = 12;
            row.style.borderBottomLeftRadius = 12;
            row.style.borderBottomRightRadius = 12;

            if (round.isTimeout)
                row.style.backgroundColor = new StyleColor(new Color(0.4f, 0.4f, 0.4f, 0.3f));
            else if (round.isCorrect)
                row.style.backgroundColor = new StyleColor(new Color(0.06f, 0.44f, 0.31f, 0.4f));
            else
                row.style.backgroundColor = new StyleColor(new Color(0.56f, 0.16f, 0.16f, 0.4f));

            // Image Thumbnail (if exists)
            if (!string.IsNullOrEmpty(round.imageUrl))
            {
                VisualElement img = new VisualElement();
                img.style.width = 40;
                img.style.height = 40;
                img.style.marginRight = 12;
                img.style.borderTopLeftRadius = 6;
                img.style.borderTopRightRadius = 6;
                img.style.borderBottomLeftRadius = 6;
                img.style.borderBottomRightRadius = 6;
                img.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0.2f));
                img.style.flexShrink = 0;
                StartCoroutine(DownloadAndSetImage(round.imageUrl, img));
                row.Add(img);
            }

            // Info Column
            VisualElement info = new VisualElement();
            info.style.flexGrow = 1;

            string icon = round.isTimeout ? "⏱" : round.isCorrect ? "✅" : "❌";
            Label qLbl = new Label($"{icon}  {round.question}");
            qLbl.style.color = Color.white;
            qLbl.style.fontSize = 16;
            qLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            qLbl.style.whiteSpace = WhiteSpace.Normal;
            qLbl.style.marginBottom = 4;
            info.Add(qLbl);

            // Always show correct answer
            Label ansLbl = new Label($"Đáp án đúng: {round.correctAnswer}");
            ansLbl.style.color = round.isCorrect ? new StyleColor(new Color(0.40f, 0.93f, 0.60f)) : new StyleColor(new Color(0.93f, 0.40f, 0.40f));
            ansLbl.style.fontSize = 13;
            ansLbl.style.whiteSpace = WhiteSpace.Normal;
            ansLbl.style.marginBottom = 2;
            info.Add(ansLbl);

            if (!round.isCorrect && !round.isTimeout)
            {
                Label yourLbl = new Label($"Bạn chọn: {(string.IsNullOrEmpty(round.playerAnswer) ? "..." : round.playerAnswer)}");
                yourLbl.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
                yourLbl.style.fontSize = 11;
                yourLbl.style.whiteSpace = WhiteSpace.Normal;
                info.Add(yourLbl);
            }

            row.Add(info);
            return row;
        }

        private void StartBattle(bool isRanked)
        {
            _isRankedBattle = isRanked;
            _selectedBattleItems.Clear();

            if (_isRankedBattle)
            {
                // Ranked mode: start LAN matchmaking
                StartLANMatchmaking();
            }
            else
            {
                // Casual mode: Start offline match directly (no item selection)
                if (BattleGameplayScreenAsset == null)
                {
                    Debug.LogError("🚨 LỖI: BattleGameplayScreenAsset chưa được gán!");
                    return;
                }
                LoadScreen(BattleGameplayScreenAsset);
            }
        }

        private void StartLANMatchmaking()
        {
            if (_jsonDb == null || _jsonDb.currentUser == null)
            {
                Debug.LogError("Không có thông tin người dùng để tìm trận!");
                return;
            }

            var curUser = _jsonDb.currentUser;

            // 1. Tạo giao diện ghép trận tối giản
            _matchmakingOverlay = new VisualElement();
            _matchmakingOverlay.style.position = Position.Absolute;
            _matchmakingOverlay.style.width = Length.Percent(100);
            _matchmakingOverlay.style.height = Length.Percent(100);
            _matchmakingOverlay.style.backgroundColor = new Color(0.05f, 0.08f, 0.15f, 0.9f);
            _matchmakingOverlay.style.justifyContent = Justify.Center;
            _matchmakingOverlay.style.alignItems = Align.Center;

            VisualElement dialog = new VisualElement();
            dialog.style.width = 280;
            dialog.style.backgroundColor = new Color(0.08f, 0.12f, 0.22f);
            dialog.style.borderTopLeftRadius = 16;
            dialog.style.borderTopRightRadius = 16;
            dialog.style.borderBottomLeftRadius = 16;
            dialog.style.borderBottomRightRadius = 16;
            dialog.style.paddingTop = 28;
            dialog.style.paddingBottom = 28;
            dialog.style.paddingLeft = 20;
            dialog.style.paddingRight = 20;
            dialog.style.alignItems = Align.Center;
            dialog.style.borderTopWidth = 1;
            dialog.style.borderBottomWidth = 1;
            dialog.style.borderLeftWidth = 1;
            dialog.style.borderRightWidth = 1;
            dialog.style.borderTopColor = new Color(0.23f, 0.51f, 0.96f, 0.4f);
            dialog.style.borderBottomColor = new Color(0.23f, 0.51f, 0.96f, 0.4f);
            dialog.style.borderLeftColor = new Color(0.23f, 0.51f, 0.96f, 0.4f);
            dialog.style.borderRightColor = new Color(0.23f, 0.51f, 0.96f, 0.4f);

            Label title = new Label("ĐANG TÌM ĐỐI THỦ...");
            title.name = "MatchmakingTitle";
            title.style.fontSize = 18;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.marginBottom = 24;
            dialog.Add(title);

            // Spinner xoay vòng (sử dụng các ký tự ASCII cơ bản để đảm bảo font nào cũng hiển thị được)
            Label animation = new Label("|");
            animation.name = "MatchmakingSpinner";
            animation.style.fontSize = 42;
            animation.style.color = new StyleColor(new Color(0.23f, 0.51f, 0.96f));
            animation.style.marginBottom = 24;
            dialog.Add(animation);

            Button btnCancel = new Button();
            btnCancel.text = "HỦY";
            btnCancel.name = "BtnCancelMatchmake";
            btnCancel.AddToClassList("btn-secondary");
            btnCancel.style.width = Length.Percent(70);
            btnCancel.style.height = 36;
            btnCancel.clicked += CancelLANMatchmaking;
            dialog.Add(btnCancel);

            _matchmakingOverlay.Add(dialog);
            _matchmakingOverlay.name = "LANMatchmakingOverlay";
            _root.Add(_matchmakingOverlay);

            // Animate text spinner
            string[] spinnerFrames = { "|", "/", "-", "\\" };
            int frameIndex = 0;
            animation.schedule.Execute(() =>
            {
                animation.text = spinnerFrames[frameIndex];
                frameIndex = (frameIndex + 1) % spinnerFrames.Length;
            }).Every(80);

            // 2. Gọi API Matchmake và bắt đầu Polling
            string avatar = GetUserAvatar(curUser.id);
            VocabLearning.Network.NetworkClient.Instance.Matchmake(curUser.id, curUser.username, curUser.rankPoints, avatar, (success, msg, res) =>
            {
                if (success && res != null && res.success)
                {
                    if (res.status == "matched")
                    {
                        StartCoroutine(MatchFoundTransitionCoroutine(res.roomId));
                    }
                    else
                    {
                        _matchmakingCoroutine = StartCoroutine(LANMatchmakePollingCoroutine());
                    }
                }
                else
                {
                    Debug.LogError($"Lỗi khi bắt đầu ghép trận: {msg}");
                    CancelLANMatchmaking();
                }
            });
        }

        private IEnumerator MatchFoundTransitionCoroutine(string roomId)
        {
            // Dừng vòng lặp polling
            if (_matchmakingCoroutine != null)
            {
                StopCoroutine(_matchmakingCoroutine);
                _matchmakingCoroutine = null;
            }

            // Cập nhật giao diện sang trạng thái Tìm Thấy Đối Thủ
            if (_matchmakingOverlay != null)
            {
                Label title = _matchmakingOverlay.Q<Label>("MatchmakingTitle");
                if (title != null)
                {
                    title.text = "ĐÃ TÌM THẤY ĐỐI THỦ!";
                    title.style.color = new StyleColor(new Color(0.06f, 0.73f, 0.51f)); // Green
                }

                Label spinner = _matchmakingOverlay.Q<Label>("MatchmakingSpinner");
                if (spinner != null)
                {
                    spinner.text = "⚔️ VS ⚔️";
                    spinner.style.fontSize = 28;
                }

                Button btnCancel = _matchmakingOverlay.Q<Button>("BtnCancelMatchmake");
                if (btnCancel != null)
                {
                    btnCancel.style.display = DisplayStyle.None; // Ẩn nút Hủy
                }
            }

            SoundManager.PlayCorrect();

            // Chờ 2 giây thông báo trước khi vào game
            yield return new WaitForSeconds(2.0f);

            EnterLANRoom(roomId);
        }

        private IEnumerator LANMatchmakePollingCoroutine()
        {
            var curUser = _jsonDb.currentUser;
            while (true)
            {
                yield return new WaitForSeconds(0.1f);

                bool responded = false;
                VocabLearning.Network.NetworkClient.Instance.GetMatchmakeStatus(curUser.id, (success, msg, res) =>
                {
                    responded = true;
                    if (success && res != null && res.success)
                    {
                        if (res.status == "matched")
                        {
                            StartCoroutine(MatchFoundTransitionCoroutine(res.roomId));
                        }
                    }
                });

                yield return new WaitUntil(() => responded);
            }
        }

        private void CancelLANMatchmaking()
        {
            if (_matchmakingCoroutine != null)
            {
                StopCoroutine(_matchmakingCoroutine);
                _matchmakingCoroutine = null;
            }

            if (_matchmakingOverlay != null)
            {
                _root.Remove(_matchmakingOverlay);
                _matchmakingOverlay = null;
            }

            if (_jsonDb != null && _jsonDb.currentUser != null)
            {
                VocabLearning.Network.NetworkClient.Instance.CancelMatchmake(_jsonDb.currentUser.id, (success, msg, res) => {
                    Debug.Log("Đã gửi yêu cầu hủy tìm trận lên server.");
                });
            }
        }

        private void EnterLANRoom(string roomId)
        {
            if (_matchmakingCoroutine != null) StopCoroutine(_matchmakingCoroutine);
            _matchmakingCoroutine = null;

            if (_matchmakingOverlay != null)
            {
                _root.Remove(_matchmakingOverlay);
                _matchmakingOverlay = null;
            }

            _pvpRoomId = roomId;
            Debug.Log($"🎯 [LAN PVP] Vào phòng đấu: {roomId}");
            LoadScreen(BattleGameplayScreenAsset);
        }

        private void BindBattleLoadoutEvents()
        {
            Button backBtn = _root.Q<Button>("BtnBack");
            if (backBtn != null) backBtn.clicked += () => LoadScreen(BattleScreenAsset);

            RenderLoadoutItems();
            UpdateLoadoutStatus();
        }

        private void RenderLoadoutItems()
        {
            ScrollView list = _root.Q<ScrollView>("LoadoutItemsList");
            if (list == null || _jsonDb == null) return;
            list.Clear();

            System.Collections.Generic.List<VocabLearning.Data.InventoryItemJson> consumables = _jsonDb.inventory.FindAll(i => i.category == "Consumable" && i.isCombatItem && i.quantity > 0);

            foreach (var item in consumables)
            {
                VisualElement card = new VisualElement();
                card.AddToClassList("loadout-item");
                if (_selectedBattleItems.Contains(item)) card.AddToClassList("loadout-item-selected");

                Label icon = new Label(item.icon);
                icon.style.fontSize = 24;
                icon.style.marginRight = 12;
                card.Add(icon);

                VisualElement info = new VisualElement();
                info.style.flexGrow = 1;
                Label name = new Label(item.name);
                name.AddToClassList("font-bold");
                name.style.color = Color.white;
                Label desc = new Label(item.description);
                desc.AddToClassList("text-muted");
                desc.style.fontSize = 11;
                info.Add(name);
                info.Add(desc);
                card.Add(info);

                Label qty = new Label($"x{item.quantity}");
                qty.AddToClassList("font-bold");
                qty.style.color = new StyleColor(new Color(0.23f, 0.51f, 0.96f));
                card.Add(qty);

                card.RegisterCallback<ClickEvent>(evt =>
                {
                    if (_selectedBattleItems.Contains(item))
                    {
                        _selectedBattleItems.Remove(item);
                        card.RemoveFromClassList("loadout-item-selected");
                    }
                    else if (_selectedBattleItems.Count < 3)
                    {
                        _selectedBattleItems.Add(item);
                        card.AddToClassList("loadout-item-selected");
                    }
                    UpdateLoadoutStatus();
                });

                list.Add(card);
            }
        }

        private void UpdateLoadoutStatus()
        {
            Label count = _root.Q<Label>("SelectionCount");
            if (count != null) count.text = $"Selected: {_selectedBattleItems.Count}/3";

            Button btnStart = _root.Q<Button>("BtnConfirmStart");
            if (btnStart != null)
            {
                // In UI Toolkit, we can't easily "disable" visually without class
                if (_selectedBattleItems.Count > 0)
                {
                    btnStart.style.backgroundColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f)); // Success green
                    btnStart.style.color = Color.white;
                    btnStart.clicked -= LoadBattleFromLoadout; // Clean up
                    btnStart.clicked += LoadBattleFromLoadout;
                }
                else
                {
                    btnStart.style.backgroundColor = new StyleColor(new Color(0.2f, 0.25f, 0.35f));
                    btnStart.style.color = new StyleColor(new Color(1f, 1f, 1f, 0.3f));
                    btnStart.clicked -= LoadBattleFromLoadout;
                }
            }
        }

        private void LoadBattleFromLoadout()
        {
            if (_selectedBattleItems.Count == 0) return;
            if (BattleGameplayScreenAsset == null) return;
            LoadScreen(BattleGameplayScreenAsset);
        }

        private void BindBattleGameplayEvents()
        {
            if (_isRankedBattle)
            {
                StartLANPvPGameplay();
            }
            else
            {
                SetupOfflineBotMatch();
            }
        }

        private void SetupOfflineBotMatch()
        {
            Button btnFlee = _root.Q<Button>("BtnFlee");
            if (btnFlee != null)
            {
                btnFlee.clicked += () =>
                {
                    ShowConfirmationDialog(
                        "Thoát trận đấu?",
                        "Bạn có chắc chắn muốn bỏ chạy? Bạn sẽ bị tính là thua cuộc!",
                        () =>
                        {
                            StopBattleTimer();
                            _battlePlayerHP = 0;
                            FinishBattle(false);
                        },
                        null
                    );
                };
            }

            Label lblMode = _root.Q<Label>("LblBattleMode");
            if (lblMode != null) lblMode.text = "CASUAL MATCH";

            // Initialize Stats
            _battlePlayerHP = 100;
            _battleEnemyHP = 100;
            _battleShieldActive = false;

            // Thiết lập thông tin đối thủ là Bot AI
            int myPoints = (_jsonDb != null && _jsonDb.currentUser != null) ? _jsonDb.currentUser.rankPoints : 1000;
            _battleEnemyData = new VocabLearning.Data.UserJson
            {
                id = "bot_ai",
                username = "Smart Bot [AI]",
                displayName = "Smart Bot [AI]",
                rankPoints = myPoints, // Có cùng điểm Rank như người chơi để độ khó tương đương
                wins = 0,
                totalGames = 0,
                inventory = new List<VocabLearning.Data.InventoryItemJson>()
            };

            Debug.Log($"🎯 [Casual Match] Khởi tạo trận đấu Offline với Smart Bot [AI] ({myPoints} pts).");

            // [RANK] Build word pool theo rank người chơi
            _currentBattleRounds.Clear(); // Reset lịch sử câu hỏi cho trận mới
            BuildBattleWordPool();

            // [RANK] Hiển thị rank pool info lên UI
            if (_jsonDb != null && _jsonDb.currentUser != null)
            {
                string playerRankKey = GetRankKey(_jsonDb.currentUser.rankPoints);
                int playerRankOrder = GetRankOrder(playerRankKey);
                string[] rankNames = { "Đồng", "Bạc", "Vàng", "Bạch Kim", "Kim Cương", "Siêu Cấp" };
                System.Text.StringBuilder poolLabel = new System.Text.StringBuilder();
                for (int r = 0; r <= playerRankOrder && r < rankNames.Length; r++)
                {
                    if (poolLabel.Length > 0) poolLabel.Append(" + ");
                    poolLabel.Append(rankNames[r]);
                }
                Label lblPool = _root.Q<Label>("LblRankPool");
                if (lblPool != null) lblPool.text = $"📚 Pool: {poolLabel}";
                Debug.Log($"[Battle] Rank Pool built for {playerRankKey}: {_battleWordPool.Count} words total.");
            }

            RenderBattleSkills();
            UpdateBattleUI();

            // Initial Binding for answer buttons
            for (int i = 0; i < 4; i++)
            {
                Button btn = _root.Q<Button>($"BtnAns{i}");
                if (btn != null)
                {
                    btn.clicked += () => OnBattleAnswer(btn.text);
                }
            }

            NextBattleQuestion();
        }

        private void StartLANPvPGameplay()
        {
            Button btnFlee = _root.Q<Button>("BtnFlee");
            if (btnFlee != null)
            {
                btnFlee.clicked += () =>
                {
                    ShowConfirmationDialog(
                        "Thoát trận đấu?",
                        "Bạn có chắc chắn muốn bỏ chạy? Bạn sẽ bị tính là thua cuộc!",
                        () =>
                        {
                            StartCoroutine(LeaveLANRoomCoroutine());
                        },
                        null
                    );
                };
            }

            Label lblMode = _root.Q<Label>("LblBattleMode");
            if (lblMode != null) lblMode.text = "RANKED MATCH";

            _currentBattleRounds.Clear();

            // Khởi tạo binding cho các nút đáp án (sử dụng OnLANBattleAnswer)
            for (int i = 0; i < 4; i++)
            {
                Button btn = _root.Q<Button>($"BtnAns{i}");
                if (btn != null)
                {
                    btn.clicked += () => OnLANBattleAnswer(btn.text);
                }
            }

            _pvpGameplayCoroutine = StartCoroutine(LANPvPGameplayLoopCoroutine());
        }

        private IEnumerator LeaveLANRoomCoroutine()
        {
            StopBattleTimer();
            if (_pvpGameplayCoroutine != null)
            {
                StopCoroutine(_pvpGameplayCoroutine);
                _pvpGameplayCoroutine = null;
            }

            bool responded = false;
            var curUser = _jsonDb.currentUser;
            VocabLearning.Network.NetworkClient.Instance.LeaveBattleRoom(_pvpRoomId, curUser.id, (success, msg, res) =>
            {
                responded = true;
            });
            yield return new WaitUntil(() => responded);

            _battlePlayerHP = 0;
            FinishBattle(false);
        }

        private IEnumerator LANPvPGameplayLoopCoroutine()
        {
            bool roomLoaded = false;
            var curUser = _jsonDb.currentUser;
            
            VocabLearning.Network.NetworkClient.Instance.GetRoomState(_pvpRoomId, (success, msg, res) =>
            {
                if (success && res != null && res.success)
                {
                    var opponentState = res.room.players.Find(p => p.userId != curUser.id);
                    if (opponentState != null)
                    {
                        _battleEnemyData = new VocabLearning.Data.UserJson
                        {
                            id = opponentState.userId,
                            username = opponentState.username,
                            displayName = opponentState.username,
                            rankPoints = opponentState.rankPoints
                        };
                    }
                    
                    _battleWordPool.Clear();
                    _battleWordPool.AddRange(res.room.wordPool);
                    _battlePoolIndex = 0;

                    _battleAllPoolMeanings.Clear();
                    _battleAllPoolWords.Clear();
                    foreach (var w in _battleWordPool)
                    {
                        if (!_battleAllPoolMeanings.Contains(w.meaning)) _battleAllPoolMeanings.Add(w.meaning);
                        if (!_battleAllPoolWords.Contains(w.word)) _battleAllPoolWords.Add(w.word);
                    }

                    roomLoaded = true;
                }
                else
                {
                    Debug.LogError($"Không thể tải thông tin phòng LAN: {msg}");
                }
            });

            yield return new WaitUntil(() => roomLoaded);

            _battlePlayerHP = 100;
            _battleEnemyHP = 100;
            _battleShieldActive = false;
            
            RenderBattleSkills();
            UpdateBattleUI();

            Label lblPool = _root.Q<Label>("LblRankPool");
            if (lblPool != null) lblPool.text = "📚 Pool: LAN PvP Room Pool";

            // Vòng lặp các câu hỏi
            while (_battlePlayerHP > 0 && _battleEnemyHP > 0 && _battlePoolIndex < _battleWordPool.Count)
            {
                yield return StartCoroutine(LANPvPRoundCoroutine(_battlePoolIndex));
            }
        }

        private IEnumerator LANPvPRoundCoroutine(int roundIndex)
        {
            _playerAnswered = false;
            _playerAnswerText = "";

            // Reset trạng thái hiển thị của các nút đáp án
            for (int i = 0; i < 4; i++)
            {
                Button btn = _root.Q<Button>($"BtnAns{i}");
                if (btn != null)
                {
                    btn.SetEnabled(true);
                    btn.style.opacity = 1f;
                    btn.style.borderTopColor = new StyleColor(StyleKeyword.Null);
                    btn.style.borderBottomColor = new StyleColor(StyleKeyword.Null);
                    btn.style.borderLeftColor = new StyleColor(StyleKeyword.Null);
                    btn.style.borderRightColor = new StyleColor(StyleKeyword.Null);
                    btn.style.borderTopWidth = new StyleFloat(StyleKeyword.Null);
                    btn.style.borderBottomWidth = new StyleFloat(StyleKeyword.Null);
                    btn.style.borderLeftWidth = new StyleFloat(StyleKeyword.Null);
                    btn.style.borderRightWidth = new StyleFloat(StyleKeyword.Null);
                }
            }

            VocabLearning.Data.WordJson current = _battleWordPool[roundIndex];
            _battleCurrentWord = current;

            Label qPrompt = _root.Q<Label>("BattlePromptText");
            Label qText = _root.Q<Label>("BattleQuestionText");
            VisualElement imgFrame = _root.Q<VisualElement>("BattleQuestionImageFrame");
            VisualElement imgElem = _root.Q<VisualElement>("BattleQuestionImage");
            Label subText = _root.Q<Label>("BattleQuestionSub");

            System.Collections.Generic.List<string> options = new System.Collections.Generic.List<string>();
            System.Collections.Generic.List<string> distractorPool = new System.Collections.Generic.List<string>();

            _isImageMode = false;
            if (!string.IsNullOrEmpty(_battleCurrentWord.imageUrl))
            {
                // Quyết định chế độ hiển thị đồng bộ (Chẵn: hình ảnh, Lẻ: chữ)
                _isImageMode = (roundIndex % 2 == 0);
            }

            if (_isImageMode)
            {
                if (qPrompt != null) qPrompt.text = "What is this?";
                if (qText != null) qText.style.display = DisplayStyle.None;
                if (imgFrame != null) imgFrame.style.display = DisplayStyle.Flex;
                if (subText != null)
                {
                    subText.text = _battleCurrentWord.imageSub;
                    subText.style.display = string.IsNullOrEmpty(_battleCurrentWord.imageSub) ? DisplayStyle.None : DisplayStyle.Flex;
                }

                if (imgElem != null)
                {
                    imgElem.style.backgroundImage = null;
                    StartCoroutine(DownloadAndSetImage(_battleCurrentWord.imageUrl, imgElem));
                }

                options.Add(_battleCurrentWord.word);
                distractorPool.AddRange(_battleAllPoolWords);
                distractorPool.Remove(_battleCurrentWord.word);
            }
            else
            {
                if (qPrompt != null) qPrompt.text = "Translate this word:";
                if (qText != null)
                {
                    qText.text = _battleCurrentWord.word;
                    qText.style.display = DisplayStyle.Flex;
                }
                if (imgFrame != null) imgFrame.style.display = DisplayStyle.None;
                if (subText != null) subText.style.display = DisplayStyle.None;

                options.Add(_battleCurrentWord.meaning);
                distractorPool.AddRange(_battleAllPoolMeanings);
                distractorPool.Remove(_battleCurrentWord.meaning);
            }

            // Sắp xếp các lựa chọn sai một cách đồng bộ
            distractorPool.Sort();
            int needed = 3;
            for (int i = 0; i < distractorPool.Count && needed > 0; i++)
            {
                options.Add(distractorPool[i]);
                needed--;
            }

            while (options.Count < 4)
            {
                options.Add("...");
            }

            // Sắp xếp tất cả các phương án để hiển thị đồng nhất trên cả 2 máy
            options.Sort();

            for (int i = 0; i < 4; i++)
            {
                Button btnAns = _root.Q<Button>($"BtnAns{i}");
                if (btnAns != null)
                {
                    btnAns.text = options[i];
                }
            }

            // Bắt đầu đếm ngược thời gian
            _battleTimer = 15f;
            float pollCooldown = 0f;
            while (_battleTimer > 0 && !_playerAnswered)
            {
                _battleTimer -= Time.deltaTime;
                VisualElement fill = _root.Q<VisualElement>("TimerFill");
                if (fill != null) fill.style.width = Length.Percent((_battleTimer / 15f) * 100f);

                // Định kỳ kiểm tra (poll) xem đối thủ đã trả lời xong/thoát trận chưa
                pollCooldown -= Time.deltaTime;
                if (pollCooldown <= 0f)
                {
                    pollCooldown = 0.1f; // Poll mỗi 0.1 giây
                    StartCoroutine(CheckOpponentStatusCoroutine(roundIndex));
                }

                yield return null;
            }

            // Lấy đáp án và thời gian phản hồi thực tế
            string ansText = "";
            float timeSpent = 15f;
            if (_playerAnswered)
            {
                ansText = _playerAnswerText;
                timeSpent = 15f - _battleTimer;
            }
            else
            {
                _playerAnswered = true; // Đánh dấu đã trả lời do hết giờ
                ansText = "";
                timeSpent = 15f;
            }

            // Khóa tất cả các nút đáp án ngay lập tức để người chơi không thể click tiếp tục
            for (int i = 0; i < 4; i++)
            {
                Button btn = _root.Q<Button>($"BtnAns{i}");
                if (btn != null)
                {
                    btn.SetEnabled(false);
                    // Nếu người chơi chưa chọn gì (bị chậm hơn hoặc hết giờ), làm mờ tất cả các nút
                    if (string.IsNullOrEmpty(ansText))
                    {
                        btn.style.opacity = 0.5f;
                    }
                }
            }

            // Gửi đáp án và CHỜ cho đến khi cả hai người chơi hoàn thành lượt đấu này
            yield return StartCoroutine(SubmitLANAnswerCoroutine(ansText, timeSpent, roundIndex));
        }

        private void OnLANBattleAnswer(string answerText)
        {
            if (_playerAnswered) return;
            _playerAnswered = true;
            _playerAnswerText = answerText;

            string correctAnswer = _isImageMode ? _battleCurrentWord.word : _battleCurrentWord.meaning;
            bool isCorrect = (answerText == correctAnswer);

            for (int i = 0; i < 4; i++)
            {
                Button btn = _root.Q<Button>($"BtnAns{i}");
                if (btn != null)
                {
                    btn.SetEnabled(false);
                    if (btn.text == answerText)
                    {
                        Color borderColor = isCorrect ? new Color(0.06f, 0.73f, 0.51f) : new Color(0.93f, 0.26f, 0.26f);
                        btn.style.borderTopColor = new StyleColor(borderColor);
                        btn.style.borderBottomColor = new StyleColor(borderColor);
                        btn.style.borderLeftColor = new StyleColor(borderColor);
                        btn.style.borderRightColor = new StyleColor(borderColor);
                        btn.style.borderTopWidth = 3;
                        btn.style.borderBottomWidth = 3;
                        btn.style.borderLeftWidth = 3;
                        btn.style.borderRightWidth = 3;
                        btn.style.opacity = 1f;
                    }
                    else
                    {
                        btn.style.opacity = 0.5f;
                    }
                }
            }

            if (isCorrect) SoundManager.PlayCorrect();
            else SoundManager.PlayWrong();
        }

        private IEnumerator CheckOpponentStatusCoroutine(int roundIndex)
        {
            if (string.IsNullOrEmpty(_pvpRoomId)) yield break;
            
            bool done = false;
            VocabLearning.Network.NetworkClient.Instance.GetRoomState(_pvpRoomId, (success, msg, res) =>
            {
                if (success && res != null && res.success)
                {
                    // Nếu trận đấu đã kết thúc hoặc server đã chuyển sang lượt tiếp theo
                    if (res.room.status == "finished" || res.room.currentRoundIndex > roundIndex)
                    {
                        if (!_playerAnswered)
                        {
                            _playerAnswered = true;
                            _playerAnswerText = ""; // Xem như chưa trả lời hoặc hết giờ
                            Debug.Log("⚠️ Vòng đấu kết thúc sớm do đối thủ đã trả lời đúng trước hoặc đã thoát.");
                        }
                    }
                }
                done = true;
            });
            yield return new WaitUntil(() => done);
        }

        private IEnumerator SubmitLANAnswerCoroutine(string answerText, float elapsedSeconds, int roundIndex)
        {
            var curUser = _jsonDb.currentUser;
            bool submitDone = false;
            VocabLearning.Network.PvPRoomState updatedRoom = null;

            VocabLearning.Network.NetworkClient.Instance.SubmitBattleAnswer(_pvpRoomId, curUser.id, roundIndex, answerText, elapsedSeconds, (success, msg, res) =>
            {
                if (success && res != null && res.success)
                {
                    updatedRoom = res.room;
                }
                submitDone = true;
            });

            yield return new WaitUntil(() => submitDone);

            // Vòng lặp chờ cả 2 người chơi trả lời xong (đồng bộ từ server)
            bool bothAnswered = false;
            VocabLearning.Network.PvPRoomState polledRoom = updatedRoom;

            while (!bothAnswered)
            {
                if (polledRoom != null)
                {
                    var localState = polledRoom.players.Find(p => p.userId == curUser.id);
                    var oppState = polledRoom.players.Find(p => p.userId != curUser.id);

                    if (polledRoom.status == "finished" || polledRoom.currentRoundIndex > roundIndex || (localState != null && localState.answered && oppState != null && oppState.answered))
                    {
                        bothAnswered = true;
                        break;
                    }
                }

                yield return new WaitForSeconds(0.1f);

                bool pollDone = false;
                VocabLearning.Network.NetworkClient.Instance.GetRoomState(_pvpRoomId, (success, msg, res) =>
                {
                    if (success && res != null && res.success)
                    {
                        polledRoom = res.room;
                    }
                    pollDone = true;
                });
                yield return new WaitUntil(() => pollDone);
            }

            // Đồng bộ HP và cập nhật UI sau khi kết thúc lượt
            if (polledRoom != null)
            {
                var localPlayer = polledRoom.players.Find(p => p.userId == curUser.id);
                var oppPlayer = polledRoom.players.Find(p => p.userId != curUser.id);

                _battlePlayerHP = localPlayer.hp;
                _battleEnemyHP = oppPlayer.hp;
                UpdateBattleUI();

                 // Lưu lịch sử câu hỏi cục bộ
                 string correctAns = _isImageMode ? _battleCurrentWord.word : _battleCurrentWord.meaning;
                 var roundRecord = new VocabLearning.Data.BattleRoundRecord
                 {
                     question = _isImageMode ? (_battleCurrentWord.imageSub ?? _battleCurrentWord.word) : _battleCurrentWord.word,
                     correctAnswer = correctAns,
                     playerAnswer = answerText,
                     imageUrl = _isImageMode ? _battleCurrentWord.imageUrl : null,
                     isCorrect = (answerText == correctAns),
                     isTimeout = (answerText == "")
                 };
                 _currentBattleRounds.Add(roundRecord);

                yield return new WaitForSeconds(1.5f);

                if (polledRoom.status == "finished")
                {
                    bool isWin = (polledRoom.winnerId == curUser.id);
                    bool isDraw = (polledRoom.winnerId == null || polledRoom.winnerId == "");

                    if (isDraw)
                    {
                        StopBattleTimer();
                        if (_pvpGameplayCoroutine != null)
                        {
                            StopCoroutine(_pvpGameplayCoroutine);
                            _pvpGameplayCoroutine = null;
                        }
                        if (_nextQuestionCoroutine != null)
                        {
                            StopCoroutine(_nextQuestionCoroutine);
                            _nextQuestionCoroutine = null;
                        }

                        curUser.totalGames++;
                        curUser.exp += 15;
                        SaveJsonDatabase();

                        // Thiết lập _lastBattleRecord để ShowBattleSummaryOverlay hiển thị kết quả chính xác
                        string opponentName = _battleEnemyData != null ? (string.IsNullOrEmpty(_battleEnemyData.displayName) ? _battleEnemyData.username : _battleEnemyData.displayName) : "Unknown";
                        int correctCount = 0;
                        foreach (var r in _currentBattleRounds) if (r.isCorrect) correctCount++;

                        _lastBattleRecord = new VocabLearning.Data.BattleHistoryRecord
                        {
                            matchId = System.Guid.NewGuid().ToString(),
                            date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                            isRanked = _isRankedBattle,
                            isWin = false,
                            opponentName = opponentName,
                            playerFinalHP = _battlePlayerHP,
                            enemyFinalHP = _battleEnemyHP,
                            correctCount = correctCount,
                            totalRounds = _currentBattleRounds.Count,
                            rounds = new List<VocabLearning.Data.BattleRoundRecord>(_currentBattleRounds)
                        };

                        if (curUser.battleHistory == null)
                            curUser.battleHistory = new List<VocabLearning.Data.BattleHistoryRecord>();
                        curUser.battleHistory.Insert(0, _lastBattleRecord);
                        if (curUser.battleHistory.Count > 20)
                            curUser.battleHistory.RemoveAt(curUser.battleHistory.Count - 1);

                        ShowBattleSummaryOverlay(() =>
                        {
                            VisualElement overlay = _root.Q<VisualElement>("ResultOverlay");
                            Label title = _root.Q<Label>("ResultTitle");
                            if (title != null) { title.text = "DRAW"; title.style.color = new StyleColor(Color.gray); }
                            if (overlay != null) overlay.style.display = DisplayStyle.Flex;

                            Button btnLeave = _root.Q<Button>("BtnLeaveBattle");
                            if (btnLeave != null) btnLeave.clicked += () => LoadScreen(BattleScreenAsset);
                        });
                    }
                    else
                    {
                        FinishBattle(isWin);
                    }
                }
                else
                {
                    _battlePoolIndex = polledRoom.currentRoundIndex;
                }
            }
        }

        private void StopBattleTimer()
        {
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }
        }

        private System.Collections.IEnumerator BattleTimerRoutine()
        {
            _battleTimer = 15f;

            // Thiết lập độ khó động của Bot AI dựa trên Rank Points của đối thủ (_battleEnemyData.rankPoints)
            float minAiTimer = 4f;   // Thời gian dự phòng thấp nhất Bot có thể trả lời (tính theo giây còn lại)
            float maxAiTimer = 12f;  // Thời gian dự phòng cao nhất Bot có thể trả lời
            float accuracy = 0.70f;  // Tỷ lệ chính xác mặc định

            if (_battleEnemyData != null)
            {
                int points = _battleEnemyData.rankPoints;

                // 1. TỐC ĐỘ PHẢN XẠ: Rank càng cao Bot phản xạ trả lời càng nhanh (tức là giây còn lại lúc trả lời càng nhiều)
                if (points >= 20000)      { minAiTimer = 10.0f; maxAiTimer = 14.0f; } // Siêu Cấp: Cực nhanh (chỉ mất 1 - 5 giây để trả lời)
                else if (points >= 10000) { minAiTimer = 8.5f;  maxAiTimer = 13.0f; } // Kim Cương: Rất nhanh (mất 2 - 6.5 giây)
                else if (points >= 5000)  { minAiTimer = 7.5f;  maxAiTimer = 12.0f; } // Bạch Kim: Nhanh (mất 3 - 7.5 giây)
                else if (points >= 2500)  { minAiTimer = 6.5f;  maxAiTimer = 11.0f; } // Vàng: Khá nhanh (mất 4 - 8.5 giây)
                else if (points >= 1000)  { minAiTimer = 5.0f;  maxAiTimer = 10.0f; } // Bạc: Trung bình (mất 5 - 10 giây)
                else                      { minAiTimer = 3.0f;  maxAiTimer = 8.0f;  } // Đồng: Khá chậm (mất 7 - 12 giây để suy nghĩ)

                // 2. ĐỘ CHÍNH XÁC: Rank càng cao Bot học càng giỏi, tỉ lệ trả lời đúng càng lớn
                if (points >= 20000)      accuracy = 0.95f; // Siêu Cấp: 95% trả lời đúng
                else if (points >= 10000) accuracy = 0.90f; // Kim Cương: 90% đúng
                else if (points >= 5000)  accuracy = 0.82f; // Bạch Kim: 82% đúng
                else if (points >= 2500)  accuracy = 0.75f; // Vàng: 75% đúng
                else if (points >= 1000)  accuracy = 0.65f; // Bạc: 65% đúng
                else                      accuracy = 0.50f; // Đồng: 50% đúng (50% còn lại chọn bừa)
            }
            else if (_isRankedBattle)
            {
                accuracy = 0.85f; // Chế độ dự phòng khi không tìm thấy thông tin đối thủ đấu hạng
            }

            while (_battleTimer > 0)
            {
                _battleTimer -= Time.deltaTime;
                VisualElement fill = _root.Q<VisualElement>("TimerFill");
                if (fill != null) fill.style.width = Length.Percent((_battleTimer / 15f) * 100f);

                // Quyết định hành động của Bot AI trong thời gian chạy đếm ngược
                if (!_aiAnswered && _battleTimer < UnityEngine.Random.Range(minAiTimer, maxAiTimer))
                {
                    _aiAnswered = true;
                    _aiCorrect = UnityEngine.Random.value < accuracy;

                    if (_aiCorrect)
                    {
                        Debug.Log($"🤖 Opponent ({(_battleEnemyData != null ? _battleEnemyData.username : "AI")}) got it RIGHT first!");
                        ResolveRound("AI");
                        yield break;
                    }
                    else
                    {
                        Debug.Log($"🤖 Opponent ({(_battleEnemyData != null ? _battleEnemyData.username : "AI")}) guessed WRONG!");
                        if (_playerAnswered)
                        {
                            ResolveRound("None");
                            yield break;
                        }
                    }
                }

                yield return null;
            }

            ResolveRound("None");
        }

        private void ResolveRound(string winner)
        {
            StopBattleTimer();

            // Disable all buttons immediately
            for (int i = 0; i < 4; i++)
            {
                Button btn = _root.Q<Button>($"BtnAns{i}");
                if (btn != null) { btn.SetEnabled(false); btn.style.opacity = 0.5f; }
            }

            if (winner == "Player")
            {
                _battleEnemyHP -= 10;
                Debug.Log("✅ You were FAST and CORRECT! Enemy -10 HP");
            }
            else if (winner == "AI")
            {
                if (_battleShieldActive)
                {
                    _battleShieldActive = false;
                    Debug.Log("🛡️ Shield blocked AI damage!");
                }
                else
                {
                    _battlePlayerHP -= 10;
                }

                if (_playerAnswered)
                {
                    Debug.Log("❌ Opponent was CORRECT! You -10 HP");
                }
                else
                {
                    Debug.Log("❌ Opponent was FASTER! You -10 HP");
                }
            }
            else
            {
                // Timeout or both wrong
                string correctAnswer = _isImageMode ? _battleCurrentWord.word : _battleCurrentWord.meaning;
                bool playerWrong = _playerAnswered && (_playerAnswerText != correctAnswer);
                bool aiWrong = _aiAnswered && !_aiCorrect;

                bool isTimeout = (_battleTimer <= 0f);
                bool playerMissed = !_playerAnswered && isTimeout;
                bool aiMissed = !_aiAnswered && isTimeout;

                if (playerWrong || playerMissed)
                {
                    if (_battleShieldActive && !aiWrong && !aiMissed)
                    {
                        _battleShieldActive = false;
                    }
                    else
                    {
                        _battlePlayerHP -= 10;
                    }
                }

                if (aiWrong || aiMissed)
                {
                    _battleEnemyHP -= 10;
                }

                Debug.Log("⌛ Round Ended (Timeout/Both Wrong). Both -10 HP");
            }

            if (_battlePlayerHP < 0) _battlePlayerHP = 0;
            if (_battleEnemyHP < 0) _battleEnemyHP = 0;

            // --- Ghi lại kết quả câu hỏi này ---
            string correctAns = _isImageMode ? _battleCurrentWord.word : _battleCurrentWord.meaning;
            bool isTimeoutRound = (_battleTimer <= 0f);
            var roundRecord = new VocabLearning.Data.BattleRoundRecord
            {
                question = _isImageMode ? (_battleCurrentWord.imageSub ?? _battleCurrentWord.word) : _battleCurrentWord.word,
                correctAnswer = correctAns,
                playerAnswer = _playerAnswered ? _playerAnswerText : "",
                imageUrl = _isImageMode ? _battleCurrentWord.imageUrl : null,
                isCorrect = _playerAnswered && (_playerAnswerText == correctAns),
                isTimeout = isTimeoutRound && !_playerAnswered
            };
            _currentBattleRounds.Add(roundRecord);
            // ------------------------------------

            // Lập tức cập nhật HP và giao diện
            UpdateBattleUI();

            if (_battleEnemyHP <= 0 || _battlePlayerHP <= 0)
            {
                FinishBattle(_battleEnemyHP <= 0);
            }
            else
            {
                _nextQuestionCoroutine = StartCoroutine(WaitAndNextQuestion());
            }
        }

        private System.Collections.IEnumerator WaitAndNextQuestion()
        {
            yield return new WaitForSeconds(1.5f);
            NextBattleQuestion();
        }

        private void RenderBattleSkills()
        {
            VisualElement container = _root.Q<VisualElement>("BattleItemContainer");
            if (container == null) return;
            container.Clear();

            foreach (var item in _selectedBattleItems)
            {
                Button skillBtn = new Button { text = item.icon };
                skillBtn.AddToClassList("skill-btn");
                skillBtn.tooltip = item.name;

                skillBtn.clicked += () =>
                {
                    ApplyBattleItemEffect(item);
                    skillBtn.SetEnabled(false);
                    skillBtn.style.opacity = 0.3f;
                };

                container.Add(skillBtn);
            }
        }

        private void ApplyBattleItemEffect(VocabLearning.Data.InventoryItemJson item)
        {
            // Use item logic (deduct from inventory - in-memory for this session)
            if (item.quantity > 0)
            {
                item.quantity--;
            }

            // Apply COMBAT ONLY Effects
            if (item.name.Contains("Fire"))
            {
                _battleEnemyHP -= 25;
                Debug.Log($"🔥 Fire Potion used! Enemy -25 HP");
                SoundManager.PlayFire();    
            }
            else if (item.name.Contains("Freeze") || item.icon == "🧪")
            {
                _battleEnemyHP -= 15;
                Debug.Log($"❄️ Freeze Potion used! Enemy -15 HP");
                SoundManager.PlayPoison();
            }
            else if (item.name.Contains("Health") || item.icon == "❤️")
            {
                _battlePlayerHP += 40;
                if (_battlePlayerHP > 100) _battlePlayerHP = 100;
                SoundManager.PlayHealing();
                Debug.Log($"❤️ Health Potion used! Player +40 HP");
            }
            else if (item.name.Contains("Shield") || item.icon == "🛡️")
            {
                _battleShieldActive = true;
                Debug.Log($"🛡️ Combat Shield activated!");
                SoundManager.PlayHealing();
            }

            UpdateBattleUI();
            if (_battleEnemyHP <= 0 || _battlePlayerHP <= 0)
            {
                FinishBattle(_battleEnemyHP <= 0);
            }
        }

        private void UpdateBattleUI()
        {
            if (_jsonDb == null || _jsonDb.currentUser == null) return;
            var curUser = _jsonDb.currentUser;

            // Player UI
            Label pName = _root.Q<Label>("PlayerName");
            if (pName != null) pName.text = "You";
            Label pAvatar = _root.Q<Label>("PlayerAvatar");
            if (pAvatar != null) pAvatar.text = GetUserAvatar(curUser.id);
            var pTier = GetRankTier(curUser.rankPoints);
            Label pTierLbl = _root.Q<Label>("PlayerTier");
            if (pTierLbl != null) { pTierLbl.text = pTier.name; pTierLbl.style.color = new StyleColor(pTier.color); }

            Label pHPLbl = _root.Q<Label>("PlayerHPLbl");
            if (pHPLbl != null) pHPLbl.text = $"{_battlePlayerHP} / 100 HP";

            VisualElement pHPFill = _root.Q<VisualElement>("PlayerHPFill");
            if (pHPFill != null)
            {
                pHPFill.style.width = Length.Percent(_battlePlayerHP);
                pHPFill.style.backgroundColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f)); // Emerald-500 (#10B981)
            }
            else
            {
                Debug.LogWarning("[Battle] Không tìm thấy PlayerHPFill trong UI!");
            }

            // Enemy UI
            if (_battleEnemyData != null)
            {
                Label eName = _root.Q<Label>("EnemyName");
                if (eName != null) eName.text = string.IsNullOrEmpty(_battleEnemyData.displayName) ? _battleEnemyData.username : _battleEnemyData.displayName;
                Label eAvatar = _root.Q<Label>("EnemyAvatar");
                if (eAvatar != null) eAvatar.text = GetUserAvatar(_battleEnemyData.id);
                var eTier = GetRankTier(_battleEnemyData.rankPoints);
                Label eTierLbl = _root.Q<Label>("EnemyTier");
                if (eTierLbl != null) { eTierLbl.text = eTier.name; eTierLbl.style.color = new StyleColor(eTier.color); }

                Label eHPLbl = _root.Q<Label>("EnemyHPLbl");
                if (eHPLbl != null) eHPLbl.text = $"{_battleEnemyHP} / 100 HP";

                VisualElement eHPFill = _root.Q<VisualElement>("EnemyHPFill");
                if (eHPFill != null)
                {
                    eHPFill.style.width = Length.Percent(_battleEnemyHP);
                    eHPFill.style.backgroundColor = new StyleColor(new Color(0.94f, 0.27f, 0.27f)); // Red-500 (#EF4444)
                }
                else
                {
                    Debug.LogWarning("[Battle] Không tìm thấy EnemyHPFill trong UI!");
                }
            }
        }

        private string GetRankKey(int rankPoints)
        {
            if (rankPoints >= 20000) return "SieuCap";
            if (rankPoints >= 10000) return "KimCuong";
            if (rankPoints >= 5000) return "BachKim";
            if (rankPoints >= 2500) return "Vang";
            if (rankPoints >= 1000) return "Bac";
            return "Dong";
        }

        private int GetRankOrder(string rankKey)
        {
            switch (rankKey)
            {
                case "Dong": return 0;
                case "Bac": return 1;
                case "Vang": return 2;
                case "BachKim": return 3;
                case "KimCuong": return 4;
                case "SieuCap": return 5;
                default: return 0;
            }
        }

        private void BuildBattleWordPool()
        {
            _battleWordPool.Clear();
            _battleAllPoolMeanings.Clear();
            _battleAllPoolWords.Clear();
            _battlePoolIndex = 0;

            if (_jsonDb == null || _jsonDb.words == null || _jsonDb.currentUser == null)
            {
                Debug.LogWarning("[Battle] Bảng words trung tâm bị rỗng hoặc chưa được tải!");
                return;
            }

            string playerRankKey = GetRankKey(_jsonDb.currentUser.rankPoints);
            int playerRankOrder = GetRankOrder(playerRankKey);

            foreach (var w in _jsonDb.words)
            {
                string wordRankKey = string.IsNullOrEmpty(w.rankRequired) ? "Dong" : w.rankRequired;
                int wordRankOrder = GetRankOrder(wordRankKey);

                // Chỉ lấy từ có rank <= rank người chơi
                if (wordRankOrder <= playerRankOrder)
                {
                    _battleWordPool.Add(w);
                    if (!_battleAllPoolMeanings.Contains(w.meaning))
                        _battleAllPoolMeanings.Add(w.meaning);
                    if (!_battleAllPoolWords.Contains(w.word))
                        _battleAllPoolWords.Add(w.word);
                }
            }

            Debug.Log($"[Battle] Central words pool: {_battleWordPool.Count} từ cho rank {playerRankKey}.");

            // Shuffle toàn bộ pool (Fisher-Yates)
            ShuffleBattlePool();
        }

        private void ShuffleBattlePool()
        {
            for (int i = _battleWordPool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                VocabLearning.Data.WordJson temp = _battleWordPool[i];
                _battleWordPool[i] = _battleWordPool[j];
                _battleWordPool[j] = temp;
            }
        }

        private void NextBattleQuestion()
        {
            if (_battleWordPool == null || _battleWordPool.Count == 0)
            {
                Debug.LogWarning("[Battle] Word pool rỗng! Fallback về vocabSets.");
                return;
            }

            _playerAnswered = false;
            _aiAnswered = false;
            _playerAnswerText = "";
            StopBattleTimer();
            _timerCoroutine = StartCoroutine(BattleTimerRoutine());

            // Re-enable buttons and reset their look
            for (int i = 0; i < 4; i++)
            {
                Button btn = _root.Q<Button>($"BtnAns{i}");
                if (btn != null)
                {
                    btn.SetEnabled(true);
                    btn.style.opacity = 1f;
                    // Xóa viền màu cũ, trả lại style mặc định của USS
                    btn.style.borderTopColor = new StyleColor(StyleKeyword.Null);
                    btn.style.borderBottomColor = new StyleColor(StyleKeyword.Null);
                    btn.style.borderLeftColor = new StyleColor(StyleKeyword.Null);
                    btn.style.borderRightColor = new StyleColor(StyleKeyword.Null);
                    btn.style.borderTopWidth = new StyleFloat(StyleKeyword.Null);
                    btn.style.borderBottomWidth = new StyleFloat(StyleKeyword.Null);
                    btn.style.borderLeftWidth = new StyleFloat(StyleKeyword.Null);
                    btn.style.borderRightWidth = new StyleFloat(StyleKeyword.Null);
                }
            }

            // Lấy từ tiếp theo trong pool (không trùng trong cùng phiên)
            if (_battlePoolIndex >= _battleWordPool.Count)
            {
                // Hết pool → shuffle lại để tránh nhàm
                ShuffleBattlePool();
                _battlePoolIndex = 0;
                Debug.Log("[Battle] Pool đã hết, shuffle lại để tiếp tục.");
            }

            VocabLearning.Data.WordJson current = _battleWordPool[_battlePoolIndex];
            _battlePoolIndex++;
            _battleCurrentWord = current;

            Label qPrompt = _root.Q<Label>("BattlePromptText");
            Label qText = _root.Q<Label>("BattleQuestionText");
            VisualElement imgFrame = _root.Q<VisualElement>("BattleQuestionImageFrame");
            VisualElement imgElem = _root.Q<VisualElement>("BattleQuestionImage");
            Label subText = _root.Q<Label>("BattleQuestionSub");

            System.Collections.Generic.List<string> options = new System.Collections.Generic.List<string>();
            System.Collections.Generic.List<string> distractorPool = new System.Collections.Generic.List<string>();

            _isImageMode = false;
            if (!string.IsNullOrEmpty(_battleCurrentWord.imageUrl))
            {
                _isImageMode = (UnityEngine.Random.value > 0.5f); // 50% chance
            }

            if (_isImageMode)
            {
                if (qPrompt != null) qPrompt.text = "What is this?";
                if (qText != null) qText.style.display = DisplayStyle.None;
                if (imgFrame != null) imgFrame.style.display = DisplayStyle.Flex;
                if (subText != null)
                {
                    subText.text = _battleCurrentWord.imageSub;
                    subText.style.display = string.IsNullOrEmpty(_battleCurrentWord.imageSub) ? DisplayStyle.None : DisplayStyle.Flex;
                }

                if (imgElem != null)
                {
                    imgElem.style.backgroundImage = null; // Clear previous
                    StartCoroutine(DownloadAndSetImage(_battleCurrentWord.imageUrl, imgElem));
                }

                options.Add(_battleCurrentWord.word);
                distractorPool.AddRange(_battleAllPoolWords);
                distractorPool.Remove(_battleCurrentWord.word);
            }
            else
            {
                if (qPrompt != null) qPrompt.text = "Translate this word:";
                if (qText != null)
                {
                    qText.text = _battleCurrentWord.word;
                    qText.style.display = DisplayStyle.Flex;
                }
                if (imgFrame != null) imgFrame.style.display = DisplayStyle.None;
                if (subText != null) subText.style.display = DisplayStyle.None;

                options.Add(_battleCurrentWord.meaning);
                distractorPool.AddRange(_battleAllPoolMeanings);
                distractorPool.Remove(_battleCurrentWord.meaning);
            }

            // Shuffle distractor pool
            for (int i = distractorPool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                string tmp = distractorPool[i];
                distractorPool[i] = distractorPool[j];
                distractorPool[j] = tmp;
            }

            // Lấy đủ 3 distractors
            int needed = 3;
            for (int i = 0; i < distractorPool.Count && needed > 0; i++)
            {
                options.Add(distractorPool[i]);
                needed--;
            }

            // Nếu pool quá nhỏ, dùng fallback placeholder
            while (options.Count < 4)
            {
                options.Add($"...");
            }

            // Shuffle options để đáp án đúng không luôn ở vị trí 0
            for (int i = 0; i < options.Count; i++)
            {
                string temp = options[i];
                int randomIndex = UnityEngine.Random.Range(i, options.Count);
                options[i] = options[randomIndex];
                options[randomIndex] = temp;
            }

            for (int i = 0; i < 4; i++)
            {
                Button btnAns = _root.Q<Button>($"BtnAns{i}");
                if (btnAns != null)
                {
                    btnAns.text = options[i];
                }
            }
        }

        private void OnBattleAnswer(string answerText)
        {
            if (_playerAnswered) return;
            _playerAnswered = true;
            _playerAnswerText = answerText;

            string correctAnswer = _isImageMode ? _battleCurrentWord.word : _battleCurrentWord.meaning;
            bool isCorrect = (answerText == correctAnswer);

            // Visual feedback: Disable buttons and highlight the selected one
            for (int i = 0; i < 4; i++)
            {
                Button btn = _root.Q<Button>($"BtnAns{i}");
                if (btn != null)
                {
                    btn.SetEnabled(false);
                    if (btn.text == answerText)
                    {
                        Color borderColor = isCorrect ? new Color(0.06f, 0.73f, 0.51f) : new Color(0.93f, 0.26f, 0.26f); // Emerald or Red
                        btn.style.borderTopColor = new StyleColor(borderColor);
                        btn.style.borderBottomColor = new StyleColor(borderColor);
                        btn.style.borderLeftColor = new StyleColor(borderColor);
                        btn.style.borderRightColor = new StyleColor(borderColor);
                        btn.style.borderTopWidth = 3;
                        btn.style.borderBottomWidth = 3;
                        btn.style.borderLeftWidth = 3;
                        btn.style.borderRightWidth = 3;
                        btn.style.opacity = 1f;
                    }
                    else
                    {
                        btn.style.opacity = 0.5f;
                    }
                }
            }

            if (isCorrect)
            {
                SoundManager.PlayCorrect();
                Debug.Log("✅ Player got it RIGHT first!");
                ResolveRound("Player");
            }
            else
            {
                SoundManager.PlayWrong();
                Debug.Log("❌ Player chose WRONG answer. Waiting for AI or Timer...");
                if (_aiAnswered && !_aiCorrect)
                {
                    ResolveRound("None");
                }
            }
        }

        private System.Collections.IEnumerator DownloadAndSetImage(string url, VisualElement targetElement)
        {
            if (string.IsNullOrEmpty(url)) yield break;

            using (UnityEngine.Networking.UnityWebRequest uwr = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Texture2D tex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(uwr);
                    targetElement.style.backgroundImage = new StyleBackground(tex);
                }
                else
                {
                    Debug.LogWarning($"[Battle] Failed to download image: {uwr.error}");
                }
            }
        }

        private void FinishBattle(bool isWin)
        {
            StopBattleTimer();
            if (_pvpGameplayCoroutine != null)
            {
                StopCoroutine(_pvpGameplayCoroutine);
                _pvpGameplayCoroutine = null;
            }
            if (_nextQuestionCoroutine != null)
            {
                StopCoroutine(_nextQuestionCoroutine);
                _nextQuestionCoroutine = null;
            }

            var curUser = _jsonDb.currentUser;
            curUser.totalGames++;

            VisualElement overlay = _root.Q<VisualElement>("ResultOverlay");
            Label title = _root.Q<Label>("ResultTitle");
            Label iconLbl = _root.Q<Label>("ResultIcon");
            Label subtext = _root.Q<Label>("ResultSubtext");
            Label expText = _root.Q<Label>("ResultExp");

            if (isWin)
            {
                curUser.wins++;
                SoundManager.PlayWinBattle();
                if (title != null) title.text = "VICTORY!";
                if (title != null) title.style.color = new StyleColor(new Color(0.10f, 0.73f, 0.51f)); // Green
                if (iconLbl != null) iconLbl.text = "🏆";

                if (_isRankedBattle)
                {
                    curUser.rankPoints += 25;
                    curUser.exp += 50;
                    curUser.coins += 50;
                    AddQuestProgressByType("CollectCoin", 50);

                    var mysteryBox = _jsonDb.inventory.Find(i => i.id == "4" || i.name == "Mystery Box");
                    if (mysteryBox != null)
                    {
                        mysteryBox.quantity++;
                        VisualElement rewardBox = _root.Q<VisualElement>("ItemRewardBox");
                        if (rewardBox != null) rewardBox.style.display = DisplayStyle.Flex;
                    }

                    AddQuestProgressByType("WinRankedBattle", 1);

                    if (subtext != null) subtext.text = "+25 Rank Points";
                    if (expText != null) expText.text = "+50 EXP / +50 Coins";
                }
                else
                {
                    VisualElement rewardBox = _root.Q<VisualElement>("ItemRewardBox");
                    if (rewardBox != null) rewardBox.style.display = DisplayStyle.None;

                    curUser.exp += 30;
                    curUser.coins += 30;
                    AddQuestProgressByType("CollectCoin", 30);
                    if (subtext != null) subtext.text = "Casual Match";
                    if (expText != null) expText.text = "+30 EXP / +30 Coins";
                }

                AddQuestProgressByType("WinBattle", 1);
                SaveJsonDatabase();
                UpdateAllCoinLabels();
            }
            else
            {   
                SoundManager.PlayGameOver();
                if (title != null) title.text = "DEFEAT";
                if (title != null) title.style.color = new StyleColor(new Color(0.93f, 0.26f, 0.26f)); // Red
                if (iconLbl != null) iconLbl.text = "💀";

                if (_isRankedBattle)
                {
                    curUser.rankPoints -= 15;
                    if (curUser.rankPoints < 0) curUser.rankPoints = 0;
                    curUser.exp += 15;
                    if (subtext != null) subtext.text = "-15 Rank Points";
                    if (expText != null) expText.text = "+15 EXP (Consolation)";
                }
                else
                {
                    curUser.exp += 10;
                    if (subtext != null) subtext.text = "Casual Match";
                    if (expText != null) expText.text = "+10 EXP (Consolation)";
                }
            }

            // [NEW] Hiển thị điểm số X/Y
            Label lblScore = _root.Q<Label>("ResultScore");
            if (lblScore != null)
            {
                int corrects = 0;
                foreach (var r in _currentBattleRounds) if (r.isCorrect) corrects++;
                lblScore.text = $"Score: {corrects} / {_currentBattleRounds.Count}";
            }

            // --- Lưu lịch sử trận đấu ---
            string opponentName = _battleEnemyData != null ? (string.IsNullOrEmpty(_battleEnemyData.displayName) ? _battleEnemyData.username : _battleEnemyData.displayName) : "Unknown";
            int correctCount = 0;
            foreach (var r in _currentBattleRounds) if (r.isCorrect) correctCount++;

            _lastBattleRecord = new VocabLearning.Data.BattleHistoryRecord
            {
                matchId = System.Guid.NewGuid().ToString(),
                date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                isRanked = _isRankedBattle,
                isWin = isWin,
                opponentName = opponentName,
                playerFinalHP = _battlePlayerHP,
                enemyFinalHP = _battleEnemyHP,
                correctCount = correctCount,
                totalRounds = _currentBattleRounds.Count,
                rounds = new List<VocabLearning.Data.BattleRoundRecord>(_currentBattleRounds)
            };

            if (curUser.battleHistory == null)
                curUser.battleHistory = new List<VocabLearning.Data.BattleHistoryRecord>();
            curUser.battleHistory.Insert(0, _lastBattleRecord); // Mới nhất lên đầu
            if (curUser.battleHistory.Count > 20) // Giới hạn 20 trận
                curUser.battleHistory.RemoveAt(curUser.battleHistory.Count - 1);
            // -----------------------------------

            // Hiện Summary trước, Result sau khi bấm nút
            ShowBattleSummaryOverlay(() =>
            {
                if (overlay != null) overlay.style.display = DisplayStyle.Flex;

                Button btnLeave = _root.Q<Button>("BtnLeaveBattle");
                if (btnLeave != null)
                {
                    btnLeave.clicked += () => LoadScreen(BattleScreenAsset);
                }
            });
        }

        private void ShowBattleSummaryOverlay(System.Action onContinue)
        {
            VisualElement summaryOverlay = _root.Q<VisualElement>("SummaryOverlay");
            if (summaryOverlay == null)
            {
                // Nếu không có overlay trong UI thì gọi callback ngay
                onContinue?.Invoke();
                return;
            }

            summaryOverlay.style.display = DisplayStyle.Flex;

            // Tiêu đề
            Label titleLbl = summaryOverlay.Q<Label>("SummaryTitle");
            if (titleLbl != null)
                titleLbl.text = _lastBattleRecord.isWin ? "🏆 Battle Summary - Victory!" : "💀 Battle Summary - Defeat";

            // Thống kê
            Label statsLbl = summaryOverlay.Q<Label>("SummaryStats");
            if (statsLbl != null)
                statsLbl.text = $"Correct: {_lastBattleRecord.correctCount} / {_lastBattleRecord.totalRounds}";

            // Danh sách câu hỏi
            VisualElement listContainer = summaryOverlay.Q<VisualElement>("SummaryList");
            if (listContainer != null)
            {
                listContainer.Clear();
                foreach (var round in _lastBattleRecord.rounds)
                {
                    VisualElement row = CreateBattleRoundRow(round);
                    listContainer.Add(row);
                }
            }

            // Nút Continue
            Button btnContinue = summaryOverlay.Q<Button>("BtnSummaryContinue");
            if (btnContinue != null)
            {
                btnContinue.clicked += () =>
                {
                    summaryOverlay.style.display = DisplayStyle.None;
                    onContinue?.Invoke();
                };
            }
        }
    }
}
