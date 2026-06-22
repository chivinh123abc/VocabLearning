using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace VocabLearning.UI
{
    public partial class VocabStudyController
    {
        private void BindShopEvents()
        {
            BindBottomNav();

            Button btnInventory = _root.Q<Button>("BtnShopToInventory");
            if (btnInventory != null) btnInventory.clicked += () => LoadScreen(InventoryScreenAsset);

            Button tabCosmetics = _root.Q<Button>("TabShopCosmetics");
            Button tabConsumables = _root.Q<Button>("TabShopConsumables");

            if (tabCosmetics != null)
            {
                tabCosmetics.clicked += () =>
                {
                    _shopCurrentTab = "Cosmetic";
                    tabCosmetics.AddToClassList("tab-btn-active");
                    if (tabConsumables != null) tabConsumables.RemoveFromClassList("tab-btn-active");
                    RenderShopList();
                };
            }

            if (tabConsumables != null)
            {
                tabConsumables.clicked += () =>
                {
                    _shopCurrentTab = "Consumable";
                    tabConsumables.AddToClassList("tab-btn-active");
                    if (tabCosmetics != null) tabCosmetics.RemoveFromClassList("tab-btn-active");
                    RenderShopList();
                };
            }

            Button btnHistory = _root.Q<Button>("BtnShopHistory");
            VisualElement historyOverlay = _root.Q<VisualElement>("ShopHistoryOverlay");
            if (btnHistory != null && historyOverlay != null)
            {
                btnHistory.clicked += () =>
                {
                    historyOverlay.style.display = DisplayStyle.Flex;
                    RenderShopHistory();
                };
            }

            Button btnCloseHistory = _root.Q<Button>("BtnCloseHistory");
            if (btnCloseHistory != null && historyOverlay != null)
            {
                btnCloseHistory.clicked += () => historyOverlay.style.display = DisplayStyle.None;
            }

            RenderShopList();
        }

        private void RenderShopList()
        {
            if (_jsonDb == null || _jsonDb.shopItems == null) return;

            VisualElement listContainer = _root.Q<VisualElement>("ShopListContainer");
            if (listContainer == null) return;

            listContainer.Clear();

            // Cập nhật số dư
            Label coinLabel = _root.Q<Label>("LblShopCoinBalance");
            if (coinLabel != null && _jsonDb.currentUser != null)
            {
                coinLabel.text = _jsonDb.currentUser.coins.ToString();
            }

            var filteredItems = _jsonDb.shopItems.FindAll(x => x.category == _shopCurrentTab);

            foreach (var item in filteredItems)
            {
                VisualElement card = new VisualElement();
                card.AddToClassList("card");
                card.style.width = new Length(48, LengthUnit.Percent);
                card.style.paddingTop = 0;
                card.style.paddingBottom = 0;
                card.style.paddingLeft = 0;
                card.style.paddingRight = 0;
                card.style.overflow = Overflow.Hidden;
                card.style.marginBottom = 16;
                // Mặc định viền tối
                card.style.borderTopColor = new StyleColor(new Color(0.2f, 0.25f, 0.33f));
                card.style.borderBottomColor = new StyleColor(new Color(0.2f, 0.25f, 0.33f));
                card.style.borderLeftColor = new StyleColor(new Color(0.2f, 0.25f, 0.33f));
                card.style.borderRightColor = new StyleColor(new Color(0.2f, 0.25f, 0.33f));

                VisualElement imgBox = new VisualElement();
                imgBox.style.height = 120;
                imgBox.style.justifyContent = Justify.Center;
                imgBox.style.alignItems = Align.Center;

                // Pick color by rarity
                Color bgColor = new Color(0.2f, 0.25f, 0.33f);
                if (item.rarity == "Common") bgColor = new Color(0.23f, 0.51f, 0.96f); // Blue
                else if (item.rarity == "Rare") bgColor = new Color(0.96f, 0.62f, 0.04f); // Orange
                else if (item.rarity == "Epic") bgColor = new Color(0.55f, 0.36f, 0.96f); // Purple
                else if (item.rarity == "Legendary") bgColor = new Color(0.93f, 0.26f, 0.26f); // Red

                imgBox.style.backgroundColor = new StyleColor(new Color(bgColor.r, bgColor.g, bgColor.b, 0.2f));

                VisualElement iconBox = new VisualElement();
                iconBox.style.width = 80;
                iconBox.style.height = 80;
                iconBox.style.backgroundColor = new StyleColor(bgColor);
                iconBox.style.borderTopLeftRadius = 16;
                iconBox.style.borderTopRightRadius = 16;
                iconBox.style.borderBottomLeftRadius = 16;
                iconBox.style.borderBottomRightRadius = 16;
                iconBox.style.justifyContent = Justify.Center;
                iconBox.style.alignItems = Align.Center;

                Label iconLabel = new Label(item.icon);
                iconLabel.style.fontSize = 40;
                iconBox.Add(iconLabel);
                imgBox.Add(iconBox);

                VisualElement infoBox = new VisualElement();
                infoBox.style.paddingTop = 12;
                infoBox.style.paddingBottom = 12;
                infoBox.style.paddingLeft = 12;
                infoBox.style.paddingRight = 12;

                Label nameLbl = new Label(item.name);
                nameLbl.AddToClassList("font-bold");
                nameLbl.style.color = Color.white;
                nameLbl.style.marginBottom = 8;
                infoBox.Add(nameLbl);

                // KIỂM TRA SỞ HỮU (Chỉ Cosmetic mới có khái niệm "Owned")
                bool isOwned = false;
                if (item.category == "Cosmetic" && _jsonDb.inventory != null)
                {
                    isOwned = _jsonDb.inventory.Exists(inv => inv.id == item.id);
                }

                if (isOwned)
                {
                    card.style.borderTopColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f)); // Green border
                    card.style.borderBottomColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f));
                    card.style.borderLeftColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f));
                    card.style.borderRightColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f));

                    VisualElement ownedBox = new VisualElement();
                    ownedBox.style.backgroundColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f, 0.2f));
                    ownedBox.style.borderTopLeftRadius = 12;
                    ownedBox.style.borderTopRightRadius = 12;
                    ownedBox.style.borderBottomLeftRadius = 12;
                    ownedBox.style.borderBottomRightRadius = 12;
                    ownedBox.style.paddingTop = 8;
                    ownedBox.style.paddingBottom = 8;
                    ownedBox.style.paddingLeft = 8;
                    ownedBox.style.paddingRight = 8;
                    ownedBox.style.alignItems = Align.Center;

                    Label ownedLbl = new Label("Owned ✓");
                    ownedLbl.style.color = new StyleColor(new Color(0.06f, 0.73f, 0.51f));
                    ownedLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                    ownedLbl.style.fontSize = 12;
                    ownedBox.Add(ownedLbl);

                    infoBox.Add(ownedBox);
                }
                else
                {
                    Button buyBtn = new Button();
                    buyBtn.AddToClassList("btn-primary");
                    buyBtn.style.marginTop = 0;
                    buyBtn.style.marginBottom = 0;
                    buyBtn.style.marginLeft = 0;
                    buyBtn.style.marginRight = 0;
                    buyBtn.style.paddingTop = 8;
                    buyBtn.style.paddingBottom = 8;
                    buyBtn.style.paddingLeft = 8;
                    buyBtn.style.paddingRight = 8;
                    buyBtn.style.flexDirection = FlexDirection.Row;
                    buyBtn.style.justifyContent = Justify.Center;
                    buyBtn.style.backgroundColor = new StyleColor(new Color(0.23f, 0.51f, 0.96f)); // Blue
                    buyBtn.style.borderTopLeftRadius = 12;
                    buyBtn.style.borderTopRightRadius = 12;
                    buyBtn.style.borderBottomLeftRadius = 12;
                    buyBtn.style.borderBottomRightRadius = 12;
                    buyBtn.style.minHeight = 30;

                    Label coinIcon = new Label("O");
                    coinIcon.style.color = new StyleColor(new Color(0.96f, 0.62f, 0.04f));
                    coinIcon.style.marginRight = 4;
                    buyBtn.Add(coinIcon);

                    Label priceLbl = new Label(item.price.ToString());
                    priceLbl.style.color = Color.white;
                    priceLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                    buyBtn.Add(priceLbl);

                    buyBtn.clicked += () =>
                    {
                        PurchaseShopItem(item);
                    };

                    infoBox.Add(buyBtn);
                }

                card.Add(imgBox);
                card.Add(infoBox);

                listContainer.Add(card);
            }
        }

        private void PurchaseShopItem(VocabLearning.Data.ShopItemJson item)
        {
            if (_jsonDb == null || _jsonDb.currentUser == null || _jsonDb.inventory == null) return;

            if (_jsonDb.currentUser.coins >= item.price)
            {
                // Trừ tiền
                _jsonDb.currentUser.coins -= item.price;

                // [NEW] Ghi lại lịch sử mua hàng
                if (_jsonDb.currentUser.shopHistory == null) _jsonDb.currentUser.shopHistory = new List<VocabLearning.Data.ShopPurchaseRecord>();
                _jsonDb.currentUser.shopHistory.Insert(0, new VocabLearning.Data.ShopPurchaseRecord
                {
                    itemName = item.name,
                    price = item.price,
                    date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm")
                });

                // Kiểm tra xem đã có trong kho chưa
                var invItem = _jsonDb.inventory.Find(i => i.id == item.id);
                if (invItem != null)
                {
                    if (invItem.category == "Consumable")
                    {
                        invItem.quantity++; // Tăng số lượng nếu là vật phẩm tiêu hao
                    }
                }
                else
                {
                    // Tạo mới
                    _jsonDb.inventory.Add(new VocabLearning.Data.InventoryItemJson()
                    {
                        id = item.id, // Dùng ID từ shop_items để khớp FK
                        icon = item.icon,
                        name = item.name,
                        description = item.description,
                        quantity = 1,
                        rarity = item.rarity,
                        category = item.category,
                        equipType = item.equipType,
                        isEquipped = false
                    });
                }

                _jsonDb.currentUser.inventory = _jsonDb.inventory;
                SaveJsonDatabase(); // Đồng bộ tiến trình lên SQL Server và lưu đĩa
                UpdateAllCoinLabels(); // Cập nhật lại tất cả nhãn vàng ở các màn hình khác
                RenderShopList(); // render lại để cập nhật balance và UI Owned
            }
            else
            {
                Debug.Log("Không đủ tiền mua vật phẩm này!");
            }
        }

        private void RenderShopHistory()
        {
            if (_jsonDb == null || _jsonDb.currentUser == null) return;

            VisualElement listContainer = _root.Q<VisualElement>("HistoryListContainer");
            if (listContainer == null) return;

            listContainer.Clear();

            if (_jsonDb.currentUser.shopHistory == null || _jsonDb.currentUser.shopHistory.Count == 0)
            {
                Label emptyLbl = new Label("Chưa có lịch sử mua hàng.");
                emptyLbl.style.color = new StyleColor(new Color(0.58f, 0.64f, 0.72f));
                emptyLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                emptyLbl.style.marginTop = 40;
                listContainer.Add(emptyLbl);
                return;
            }

            foreach (var record in _jsonDb.currentUser.shopHistory)
            {
                VisualElement row = new VisualElement();
                row.AddToClassList("card");
                row.style.marginBottom = 12;
                row.style.flexDirection = FlexDirection.Row;
                row.style.justifyContent = Justify.SpaceBetween;
                row.style.alignItems = Align.Center;
                row.style.paddingLeft = 16;
                row.style.paddingRight = 16;
                row.style.paddingTop = 12;
                row.style.paddingBottom = 12;

                VisualElement leftGroup = new VisualElement();
                Label nameLbl = new Label(record.itemName);
                nameLbl.AddToClassList("font-bold");
                nameLbl.style.color = Color.white;
                nameLbl.style.fontSize = 15;
                leftGroup.Add(nameLbl);

                Label dateLbl = new Label(record.date);
                dateLbl.style.color = new StyleColor(new Color(0.58f, 0.64f, 0.72f));
                dateLbl.style.fontSize = 11;
                leftGroup.Add(dateLbl);

                VisualElement rightGroup = new VisualElement();
                rightGroup.style.flexDirection = FlexDirection.Row;
                rightGroup.style.alignItems = Align.Center;

                Label coinIcon = new Label("O");
                coinIcon.style.color = new StyleColor(new Color(0.96f, 0.62f, 0.04f));
                coinIcon.style.marginRight = 4;
                rightGroup.Add(coinIcon);

                Label priceLbl = new Label(record.price.ToString());
                priceLbl.AddToClassList("font-bold");
                priceLbl.style.color = Color.white;
                priceLbl.style.fontSize = 16;
                rightGroup.Add(priceLbl);

                row.Add(leftGroup);
                row.Add(rightGroup);
                listContainer.Add(row);
            }
        }

        private void BindInventoryEvents()
        {
            Button backBtn = _root.Q<Button>("BtnBack");
            if (backBtn != null) backBtn.clicked += () => LoadScreen(ProfileScreenAsset);

            Button tabConsumables = _root.Q<Button>("TabConsumables");
            Button tabCosmetics = _root.Q<Button>("TabCosmetics");

            if (tabConsumables != null)
            {
                tabConsumables.clicked += () =>
                {
                    _inventoryCurrentTab = "Consumable";
                    tabConsumables.AddToClassList("tab-btn-active");
                    if (tabCosmetics != null) tabCosmetics.RemoveFromClassList("tab-btn-active");
                    RenderInventoryList();
                };
            }

            if (tabCosmetics != null)
            {
                tabCosmetics.clicked += () =>
                {
                    _inventoryCurrentTab = "Cosmetic";
                    tabCosmetics.AddToClassList("tab-btn-active");
                    if (tabConsumables != null) tabConsumables.RemoveFromClassList("tab-btn-active");
                    RenderInventoryList();
                };
            }

            // Init state
            if (_inventoryCurrentTab == "Consumable" && tabConsumables != null && tabCosmetics != null)
            {
                tabConsumables.AddToClassList("tab-btn-active");
                tabCosmetics.RemoveFromClassList("tab-btn-active");
            }
            else if (_inventoryCurrentTab == "Cosmetic" && tabConsumables != null && tabCosmetics != null)
            {
                tabCosmetics.AddToClassList("tab-btn-active");
                tabConsumables.RemoveFromClassList("tab-btn-active");
            }

            RenderInventoryList();
        }

        private void RenderInventoryList()
        {
            if (_jsonDb == null || _jsonDb.inventory == null) return;

            VisualElement listContainer = _root.Q<VisualElement>("InventoryListContainer");
            if (listContainer == null) return;

            listContainer.Clear();

            var filteredItems = _jsonDb.inventory.FindAll(i => i.category == _inventoryCurrentTab);

            foreach (var item in filteredItems)
            {
                VisualElement card = new VisualElement();
                card.AddToClassList("card");
                card.style.width = new Length(48, LengthUnit.Percent); // Two columns
                card.style.marginBottom = 16;
                card.style.alignItems = Align.Center;
                card.style.paddingTop = 16;
                card.style.paddingBottom = 16;
                card.style.paddingLeft = 12;
                card.style.paddingRight = 12;

                // Rarity Border Color
                Color rarityColor = new Color(0.58f, 0.64f, 0.72f); // Default/Common
                if (item.rarity == "Rare") rarityColor = new Color(0.23f, 0.51f, 0.96f); // Blue
                else if (item.rarity == "Epic") rarityColor = new Color(0.55f, 0.36f, 0.96f); // Purple
                else if (item.rarity == "Legendary") rarityColor = new Color(0.96f, 0.62f, 0.04f); // Orange

                card.style.borderTopColor = new StyleColor(rarityColor);
                card.style.borderBottomColor = new StyleColor(rarityColor);
                card.style.borderLeftColor = new StyleColor(rarityColor);
                card.style.borderRightColor = new StyleColor(rarityColor);
                card.style.borderTopWidth = 1;
                card.style.borderBottomWidth = 1;
                card.style.borderLeftWidth = 1;
                card.style.borderRightWidth = 1;

                // Icon box
                VisualElement iconBox = new VisualElement();
                iconBox.style.width = 64;
                iconBox.style.height = 64;
                iconBox.style.borderTopLeftRadius = 32;
                iconBox.style.borderTopRightRadius = 32;
                iconBox.style.borderBottomLeftRadius = 32;
                iconBox.style.borderBottomRightRadius = 32;
                iconBox.style.justifyContent = Justify.Center;
                iconBox.style.alignItems = Align.Center;
                iconBox.style.marginBottom = 12;
                iconBox.style.backgroundColor = new StyleColor(new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.2f));

                Label iconLbl = new Label(item.icon);
                iconLbl.style.fontSize = 28;
                iconBox.Add(iconLbl);

                if (_inventoryCurrentTab == "Consumable")
                {
                    // Quantity Badge
                    VisualElement badge = new VisualElement();
                    badge.style.position = Position.Absolute;
                    badge.style.top = -5;
                    badge.style.right = -5;
                    badge.style.backgroundColor = new StyleColor(new Color(0.9f, 0.2f, 0.2f)); // Red badge
                    badge.style.borderTopLeftRadius = 10;
                    badge.style.borderTopRightRadius = 10;
                    badge.style.borderBottomLeftRadius = 10;
                    badge.style.borderBottomRightRadius = 10;
                    badge.style.paddingLeft = 6;
                    badge.style.paddingRight = 6;
                    badge.style.paddingTop = 2;
                    badge.style.paddingBottom = 2;

                    Label qtyLbl = new Label($"x{item.quantity}");
                    qtyLbl.style.color = Color.white;
                    qtyLbl.style.fontSize = 10;
                    qtyLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                    badge.Add(qtyLbl);
                    iconBox.Add(badge);
                }

                card.Add(iconBox);

                // Title
                Label titleLbl = new Label(item.name);
                titleLbl.AddToClassList("font-bold");
                titleLbl.style.color = Color.white;
                titleLbl.style.fontSize = 14;
                titleLbl.style.marginBottom = 4;
                titleLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                card.Add(titleLbl);

                // Description
                Label descLbl = new Label(item.description);
                descLbl.AddToClassList("text-muted");
                descLbl.style.fontSize = 11;
                descLbl.style.whiteSpace = WhiteSpace.Normal;
                descLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                descLbl.style.marginBottom = 8;
                card.Add(descLbl);

                if (_inventoryCurrentTab == "Consumable" && item.name == "Mystery Box" && item.quantity > 0)
                {
                    Button openBtn = new Button();
                    openBtn.text = "Open";
                    openBtn.style.paddingTop = 6;
                    openBtn.style.paddingBottom = 6;
                    openBtn.style.paddingLeft = 16;
                    openBtn.style.paddingRight = 16;
                    openBtn.style.borderTopLeftRadius = 8;
                    openBtn.style.borderTopRightRadius = 8;
                    openBtn.style.borderBottomLeftRadius = 8;
                    openBtn.style.borderBottomRightRadius = 8;
                    openBtn.style.borderTopWidth = 0;
                    openBtn.style.borderBottomWidth = 0;
                    openBtn.style.borderLeftWidth = 0;
                    openBtn.style.borderRightWidth = 0;
                    openBtn.style.backgroundColor = new StyleColor(new Color(0.96f, 0.62f, 0.04f, 1f)); // Amber/Gold
                    openBtn.style.color = Color.white;
                    openBtn.style.unityFontStyleAndWeight = FontStyle.Bold;

                    openBtn.clicked += () => OpenMysteryBox(item);
                    card.Add(openBtn);
                }

                if (_inventoryCurrentTab == "Cosmetic")
                {
                    Button equipBtn = new Button();
                    equipBtn.text = item.isEquipped ? "Equipped" : "Equip";
                    equipBtn.style.paddingTop = 6;
                    equipBtn.style.paddingBottom = 6;
                    equipBtn.style.paddingLeft = 16;
                    equipBtn.style.paddingRight = 16;
                    equipBtn.style.borderTopLeftRadius = 8;
                    equipBtn.style.borderTopRightRadius = 8;
                    equipBtn.style.borderBottomLeftRadius = 8;
                    equipBtn.style.borderBottomRightRadius = 8;
                    equipBtn.style.borderTopWidth = 0;
                    equipBtn.style.borderBottomWidth = 0;
                    equipBtn.style.borderLeftWidth = 0;
                    equipBtn.style.borderRightWidth = 0;
                    equipBtn.style.color = Color.white;
                    equipBtn.style.unityFontStyleAndWeight = FontStyle.Bold;

                    if (item.isEquipped)
                    {
                        equipBtn.style.backgroundColor = new StyleColor(new Color(0.12f, 0.16f, 0.23f, 1f)); // Dark gray
                    }
                    else
                    {
                        equipBtn.style.backgroundColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f, 1f)); // Green primary
                    }

                    // Equip logic
                    equipBtn.clicked += () =>
                    {
                        if (!item.isEquipped)
                        {
                            // Unequip others OF THE SAME EQUIP TYPE
                            foreach (var i in _jsonDb.inventory.FindAll(x => x.category == "Cosmetic" && x.equipType == item.equipType))
                            {
                                i.isEquipped = false;
                            }

                            item.isEquipped = true;
                            _jsonDb.currentUser.inventory = _jsonDb.inventory;
                            SaveJsonDatabase(); // Đồng bộ lên SQL Server và lưu đĩa
                            RenderInventoryList(); // re-render
                        }
                    };

                    card.Add(equipBtn);
                }

                listContainer.Add(card);
            }
        }

        private void OpenMysteryBox(VocabLearning.Data.InventoryItemJson box)
        {
            if (box == null || box.quantity <= 0) return;
            box.quantity--;

            // Gacha rewards
            int r = UnityEngine.Random.Range(0, 100);
            string icon = "";
            string name = "";
            string rewardMsg = "";

            if (r < 40) // 40% nhận coin ngẫu nhiên (100 - 300 coins)
            {
                int coins = UnityEngine.Random.Range(10, 31) * 10;
                _jsonDb.currentUser.coins += coins;
                AddQuestProgressByType("CollectCoin", coins);
                icon = "💰";
                name = $"{coins} Coins";
                rewardMsg = "Bạn đã tìm thấy một túi tiền vàng!";
            }
            else if (r < 80) // 40% nhận một Battle Potion ngẫu nhiên
            {
                var combatItems = _jsonDb.inventory.FindAll(i => i.isCombatItem);
                if (combatItems.Count > 0)
                {
                    var rewardItem = combatItems[UnityEngine.Random.Range(0, combatItems.Count)];
                    rewardItem.quantity++;
                    icon = rewardItem.icon;
                    name = rewardItem.name;
                    rewardMsg = "Một vật phẩm chiến đấu hữu ích!";
                }
                else
                {
                    _jsonDb.currentUser.coins += 100;
                    AddQuestProgressByType("CollectCoin", 100);
                    icon = "💰";
                    name = "100 Coins";
                    rewardMsg = "Hộp quà chứa một ít tiền mặt.";
                }
            }
            else // 20% "Jackpot"
            {
                _jsonDb.currentUser.coins += 500;
                AddQuestProgressByType("CollectCoin", 500);
                icon = "💎";
                name = "500 Coins";
                rewardMsg = "SIÊU CẤP MAY MẮN! Phần thưởng lớn nhất!";
            }

            SaveJsonDatabase();
            UpdateAllCoinLabels();
            ShowRewardOverlay(icon, name, rewardMsg);
            RenderInventoryList();
        }

        private void ShowRewardOverlay(string icon, string title, string message)
        {
            VisualElement overlay = new VisualElement();
            overlay.AddToClassList("reward-overlay");

            VisualElement card = new VisualElement();
            card.AddToClassList("reward-card");

            Label iconLbl = new Label(icon);
            iconLbl.AddToClassList("reward-icon-anim");
            card.Add(iconLbl);

            Label titleLbl = new Label(title);
            titleLbl.AddToClassList("font-bold");
            titleLbl.style.fontSize = 24;
            titleLbl.style.color = Color.white;
            titleLbl.style.marginBottom = 8;
            card.Add(titleLbl);

            Label msgLbl = new Label(message);
            msgLbl.AddToClassList("text-muted");
            msgLbl.style.marginBottom = 24;
            msgLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            msgLbl.style.whiteSpace = WhiteSpace.Normal;
            card.Add(msgLbl);

            Button claimBtn = new Button();
            claimBtn.text = "CLAIM";
            claimBtn.AddToClassList("btn-primary");
            claimBtn.style.width = 160;
            claimBtn.clicked += () =>
            {
                _root.Remove(overlay);
            };
            card.Add(claimBtn);

            overlay.Add(card);
            _root.Add(overlay);

            // Trigger animations
            overlay.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                overlay.style.opacity = 1;
                card.style.scale = new StyleScale(new Vector2(1, 1));
            });
        }
    }
}
