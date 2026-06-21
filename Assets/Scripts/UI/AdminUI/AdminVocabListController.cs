using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using VocabLearning.Data;

namespace VocabLearning.UI
{
    public partial class VocabStudyController
    {
        // ── State ──────────────────────────────────
        private VocabSetJson    _currentEditingSet   = null;
        private HashSet<string> _pickerSelected      = new HashSet<string>();
        private string          _pickerRankFilter    = "Tất cả";

        // ═══════════════════════════════════════════
        //   ENTRY POINT
        // ═══════════════════════════════════════════
        private void BindAdminVocabListEvents()
        {
            _root.Q<Button>("btn-back")?.RegisterCallback<ClickEvent>(_ => LoadScreen(AdminScreenAsset));
            _root.Q<Button>("btn-add")?.RegisterCallback<ClickEvent>(_ => OpenAddSetModal());

            // Search main list
            var sf = _root.Q<TextField>("search-field");
            if (sf != null) sf.RegisterValueChangedCallback(_ => RefreshSetList());

            // Category dropdown
            var catDd = _root.Q<DropdownField>("filter-category");
            if (catDd != null)
            {
                var cats = new List<string> { "Tất cả" };
                if (_jsonDb?.vocabSets != null)
                    cats.AddRange(_jsonDb.vocabSets.Select(s => s.category)
                        .Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c));
                catDd.choices = cats;
                catDd.value   = "Tất cả";
                catDd.RegisterValueChangedCallback(_ => RefreshSetList());
            }

            // Tabs (Removed difficulty filters)

            // Set modal buttons
            _root.Q<Button>("btn-modal-close")?.RegisterCallback<ClickEvent>(_ => CloseSetModal());
            _root.Q<Button>("btn-modal-cancel")?.RegisterCallback<ClickEvent>(_ => CloseSetModal());
            _root.Q<Button>("btn-modal-save")?.RegisterCallback<ClickEvent>(_ => SaveSetFromModal());

            // Open picker button
            _root.Q<Button>("btn-open-picker")?.RegisterCallback<ClickEvent>(_ => OpenWordPicker());

            // Picker buttons
            _root.Q<Button>("btn-picker-close")?.RegisterCallback<ClickEvent>(_ => CloseWordPicker());
            _root.Q<Button>("btn-picker-cancel")?.RegisterCallback<ClickEvent>(_ => CloseWordPicker());
            _root.Q<Button>("btn-picker-confirm")?.RegisterCallback<ClickEvent>(_ => ConfirmWordPicker());
            _root.Q<Button>("btn-picker-select-all")?.RegisterCallback<ClickEvent>(_ => ToggleSelectAllPicker());

            // Picker search
            var ps = _root.Q<TextField>("picker-search");
            if (ps != null) ps.RegisterValueChangedCallback(_ => RefreshPickerList());

            // Picker rank filter
            var rankDd = _root.Q<DropdownField>("picker-filter-rank");
            if (rankDd != null)
            {
                rankDd.choices = new List<string> { "Tất cả", "Dong", "Bac", "Vang", "BachKim", "KimCuong", "SieuCap" };
                rankDd.value   = "Tất cả";
                rankDd.RegisterValueChangedCallback(evt => { _pickerRankFilter = evt.newValue; RefreshPickerList(); });
            }

            RefreshSetList();
        }


        // ═══════════════════════════════════════════
        //   MAIN LIST
        // ═══════════════════════════════════════════
        private void RefreshSetList()
        {
            if (_jsonDb?.vocabSets == null) return;
            var listView = _root.Q<ScrollView>("set-list");
            if (listView == null) return;
            listView.Clear();

            string query     = _root.Q<TextField>("search-field")?.value.Trim().ToLower() ?? "";
            string catFilter = _root.Q<DropdownField>("filter-category")?.value ?? "Tất cả";

            int total = _jsonDb.vocabSets.Count, filtered = 0, maxW = 0;
            var cats  = new HashSet<string>();

            foreach (var set in _jsonDb.vocabSets)
            {
                bool mQ = string.IsNullOrEmpty(query) ||
                    set.id.ToLower().Contains(query) || set.title.ToLower().Contains(query) ||
                    (set.category ?? "").ToLower().Contains(query) || (set.description ?? "").ToLower().Contains(query);
                bool mC = catFilter == "Tất cả" ||
                    (set.category ?? "").Equals(catFilter, System.StringComparison.OrdinalIgnoreCase);
                if (!mQ || !mC) continue;

                filtered++;
                if (!string.IsNullOrEmpty(set.category)) cats.Add(set.category);
                if (set.wordCount > maxW) maxW = set.wordCount;
                listView.Add(BuildSetCard(set));
            }

            SetTextValue("set-stat-total",     total.ToString());
            SetTextValue("set-stat-words",      (_jsonDb.words?.Count ?? 0).ToString());
            SetTextValue("set-stat-categories", cats.Count.ToString());
            SetTextValue("set-stat-max-words",  maxW.ToString());
            SetTextValue("set-count-label",     $"{filtered} bộ từ vựng");
        }

        private VisualElement BuildSetCard(VocabSetJson set)
        {
            var card = new VisualElement();
            card.AddToClassList("vocab-card");
            card.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target is Button b && b.ClassListContains("btn-delete")) return;
                OpenEditSetModal(set);
            });

            var thumb = new VisualElement();
            thumb.AddToClassList("vocab-thumbnail");
            thumb.style.justifyContent  = Justify.Center;
            thumb.style.alignItems      = Align.Center;
            thumb.style.backgroundColor = GetSetThumbnailColor(set.category ?? "");
            thumb.pickingMode           = PickingMode.Ignore;

            var icon = new Label(GetSetEmoji(set.category ?? ""));
            icon.style.fontSize       = 38;
            icon.style.unityTextAlign = TextAnchor.MiddleCenter;
            icon.pickingMode          = PickingMode.Ignore;
            thumb.Add(icon);

            var info    = new VisualElement(); info.AddToClassList("vocab-info"); info.pickingMode = PickingMode.Ignore;
            var nameRow = new VisualElement(); nameRow.AddToClassList("vocab-name-row"); nameRow.pickingMode = PickingMode.Ignore;

            var lblTitle = new Label(set.title); lblTitle.AddToClassList("vocab-word"); lblTitle.pickingMode = PickingMode.Ignore;
            bool isMultiLevel = set.levels != null && set.levels.Count > 1;
            string displayDiffVal = isMultiLevel ? "Multi-Level" : (set.difficulty ?? "Easy");
            var lblDiff  = new Label(GetDifficultyLabel(displayDiffVal));
            lblDiff.AddToClassList("rank-badge"); lblDiff.pickingMode = PickingMode.Ignore;
            if (displayDiffVal.Equals("Easy", System.StringComparison.OrdinalIgnoreCase)) lblDiff.AddToClassList("rank-badge--2");
            else if (displayDiffVal.Equals("Hard", System.StringComparison.OrdinalIgnoreCase)) lblDiff.AddToClassList("rank-badge--3");
            else if (displayDiffVal.Equals("Multi-Level", System.StringComparison.OrdinalIgnoreCase))
            {
                lblDiff.style.backgroundColor = new StyleColor(new Color(0.88f, 0.92f, 0.99f));
                lblDiff.style.color = new StyleColor(new Color(0.18f, 0.36f, 0.77f));
                lblDiff.style.borderTopColor = new StyleColor(new Color(0.78f, 0.85f, 0.98f));
                lblDiff.style.borderRightColor = new StyleColor(new Color(0.78f, 0.85f, 0.98f));
                lblDiff.style.borderBottomColor = new StyleColor(new Color(0.78f, 0.85f, 0.98f));
                lblDiff.style.borderLeftColor = new StyleColor(new Color(0.78f, 0.85f, 0.98f));
            }
            nameRow.Add(lblTitle); nameRow.Add(lblDiff);

            var lblDesc = new Label(set.description ?? "(Chưa có mô tả)");
            lblDesc.AddToClassList("vocab-meaning"); lblDesc.pickingMode = PickingMode.Ignore;

            string wc = set.wordCount > 0 ? set.wordCount.ToString() : (set.wordIds?.Count ?? 0).ToString();
            var lblMeta = new Label($"ID: {set.id}  |  📖 {wc} từ  |  🏅 {set.rankRequired ?? "—"}  |  🗂 {set.category ?? "—"}");
            lblMeta.AddToClassList("vocab-id"); lblMeta.pickingMode = PickingMode.Ignore;

            info.Add(nameRow); info.Add(lblDesc); info.Add(lblMeta);

            var btnDel = new Button { text = "🗑" };
            btnDel.AddToClassList("btn-delete");
            btnDel.clicked += () => DeleteSet(set);

            card.Add(thumb); card.Add(info); card.Add(btnDel);
            return card;
        }

        // ═══════════════════════════════════════════
        //   SET MODAL — THÊM / SỬA
        // ═══════════════════════════════════════════
        private void OpenAddSetModal()
        {
            _currentEditingSet = null;
            _pickerSelected.Clear();
            SetTextValue("modal-title", "Thêm Bộ Từ Vựng");
            SetInputValue("input-title", "");
            SetInputValue("input-desc",  "");
            HideSetModalError();
            ShowSetModal();
            RefreshPickerPreview();
        }

        private void OpenEditSetModal(VocabSetJson set)
        {
            _currentEditingSet = set;
            _pickerSelected    = new HashSet<string>(set.wordIds ?? new List<string>());
            SetTextValue("modal-title", "Chỉnh Sửa Bộ Từ Vựng");
            SetInputValue("input-title", set.title ?? "");
            SetInputValue("input-desc",  set.description ?? "");
            HideSetModalError();
            ShowSetModal();
            SetDropdownValue("input-difficulty", GetDifficultyLabel(set.difficulty ?? "Easy"));
            SetDropdownValue("input-category",   set.category ?? "");
            SetDropdownValue("input-rank",        set.rankRequired ?? "Dong");
            RefreshPickerPreview();
        }

        private void SaveSetFromModal()
        {
            string title    = GetInputValue("input-title").Trim();
            string desc     = GetInputValue("input-desc").Trim();
            string diffLabel= GetDropdownValue("input-difficulty");
            string category = GetDropdownValue("input-category");
            string rank     = GetDropdownValue("input-rank");

            if (string.IsNullOrEmpty(title))   { ShowSetModalError("⚠️  Tên bộ từ vựng không được để trống!"); return; }
            if (string.IsNullOrEmpty(category)) { ShowSetModalError("⚠️  Vui lòng chọn danh mục!"); return; }
            if (_pickerSelected.Count == 0)     { ShowSetModalError("⚠️  Vui lòng chọn ít nhất 1 từ vựng!"); return; }

            var wordIds  = _pickerSelected.OrderBy(id => id).ToList();
            string diff  = ParseDifficultyLabel(diffLabel);

            if (_currentEditingSet == null)
            {
                var newSet = new VocabSetJson
                {
                    id = GenerateNextSetId(), title = title, description = desc,
                    category = category, difficulty = diff, rankRequired = rank,
                    wordCount = wordIds.Count, wordIds = wordIds
                };
                AutoPartitionSetLevels(newSet);
                _jsonDb.vocabSets.Add(newSet);
            }
            else
            {
                _currentEditingSet.title       = title; 
                _currentEditingSet.description = desc;
                _currentEditingSet.category    = category; 
                _currentEditingSet.difficulty  = diff;
                _currentEditingSet.rankRequired= rank;
                _currentEditingSet.wordCount   = wordIds.Count; 
                _currentEditingSet.wordIds     = wordIds;
                AutoPartitionSetLevels(_currentEditingSet);
            }

            SaveJsonDatabase();
            CloseSetModal();
            RefreshSetList();
        }



        private void DeleteSet(VocabSetJson set)
        {
            _jsonDb.vocabSets.Remove(set);
            SaveJsonDatabase();
            RefreshSetList();
        }

        private void ShowSetModal()
        {
            SetDropdownChoices("input-difficulty", new List<string> { "Dễ", "Trung Bình", "Khó", "Đa Cấp Độ" });
            SetDropdownChoices("input-rank", new List<string> { "Dong","Bac","Vang","BachKim","KimCuong","SieuCap" });
            var cats = new List<string> { "Daily Life","Business","Education","Entertainment","Food","Health","Nature","Sports","Technology","Travel" };
            if (_jsonDb?.vocabSets != null)
                foreach (var s in _jsonDb.vocabSets)
                    if (!string.IsNullOrEmpty(s.category) && !cats.Contains(s.category)) cats.Add(s.category);
            cats.Sort();
            SetDropdownChoices("input-category", cats);
            var ov = _root.Q<VisualElement>("modal-overlay");
            if (ov != null) ov.style.display = DisplayStyle.Flex;
        }

        private void CloseSetModal()
        {
            var ov = _root.Q<VisualElement>("modal-overlay");
            if (ov != null) ov.style.display = DisplayStyle.None;
            HideSetModalError(); _currentEditingSet = null;
        }

        private void ShowSetModalError(string msg)
        {
            var lbl = _root.Q<Label>("modal-error-msg");
            if (lbl != null) { lbl.text = msg; lbl.style.display = DisplayStyle.Flex; }
        }

        private void HideSetModalError()
        {
            var lbl = _root.Q<Label>("modal-error-msg");
            if (lbl != null) lbl.style.display = DisplayStyle.None;
        }

        // ═══════════════════════════════════════════
        //   WORD PICKER
        // ═══════════════════════════════════════════
        private void OpenWordPicker()
        {
            var ps = _root.Q<TextField>("picker-search");
            if (ps != null) ps.SetValueWithoutNotify("");
            var rd = _root.Q<DropdownField>("picker-filter-rank");
            if (rd != null) rd.SetValueWithoutNotify("Tất cả");
            _pickerRankFilter = "Tất cả";
            var ov = _root.Q<VisualElement>("picker-overlay");
            if (ov != null) ov.style.display = DisplayStyle.Flex;
            RefreshPickerList();
        }

        private void CloseWordPicker()
        {
            var ov = _root.Q<VisualElement>("picker-overlay");
            if (ov != null) ov.style.display = DisplayStyle.None;
        }

        private void ConfirmWordPicker()
        {
            CloseWordPicker();
            RefreshPickerPreview();
        }

        private void ToggleSelectAllPicker()
        {
            var visible  = GetFilteredPickerWords();
            bool allSel  = visible.Count > 0 && visible.All(w => _pickerSelected.Contains(w.id));
            if (allSel) foreach (var w in visible) _pickerSelected.Remove(w.id);
            else        foreach (var w in visible) _pickerSelected.Add(w.id);
            RefreshPickerList();
        }

        private List<WordJson> GetFilteredPickerWords()
        {
            if (_jsonDb?.words == null) return new List<WordJson>();
            string q = _root.Q<TextField>("picker-search")?.value.Trim().ToLower() ?? "";
            return _jsonDb.words.Where(w =>
            {
                bool mQ = string.IsNullOrEmpty(q) ||
                    w.word.ToLower().Contains(q) || w.meaning.ToLower().Contains(q) || w.id.ToLower().Contains(q);
                bool mR = _pickerRankFilter == "Tất cả" ||
                    (w.rankRequired ?? "").Equals(_pickerRankFilter, System.StringComparison.OrdinalIgnoreCase);
                return mQ && mR;
            }).ToList();
        }

        private void RefreshPickerList()
        {
            var listView = _root.Q<ScrollView>("picker-word-list");
            if (listView == null) return;
            listView.Clear();

            var words   = GetFilteredPickerWords();
            bool allSel = words.Count > 0 && words.All(w => _pickerSelected.Contains(w.id));
            var btnAll  = _root.Q<Button>("btn-picker-select-all");
            if (btnAll != null) btnAll.text = allSel ? "Bỏ chọn tất" : "Chọn tất cả";

            foreach (var word in words)
                listView.Add(BuildPickerRow(word, _pickerSelected.Contains(word.id)));

            UpdatePickerCountLabels();
        }


        private VisualElement BuildPickerRow(WordJson word, bool isSelected)
        {
            var row = new Button();
            row.style.flexDirection  = FlexDirection.Row;
            row.style.alignItems     = Align.Center;
            row.style.paddingTop     = 14; row.style.paddingBottom  = 14;
            row.style.paddingLeft    = 16; row.style.paddingRight   = 16;
            row.style.marginBottom   = 8;
            row.style.borderTopLeftRadius     = 16; row.style.borderTopRightRadius    = 16;
            row.style.borderBottomLeftRadius  = 16; row.style.borderBottomRightRadius = 16;
            row.style.borderTopWidth    = 1.5f; row.style.borderBottomWidth = 1.5f;
            row.style.borderLeftWidth   = 1.5f; row.style.borderRightWidth  = 1.5f;
            ApplyPickerRowStyle(row, isSelected);

            // Checkbox emoji
            var checkbox = new Label(isSelected ? "☑" : "☐");
            checkbox.style.fontSize       = 34;
            checkbox.style.unityTextAlign = TextAnchor.MiddleCenter;
            checkbox.style.marginRight    = 16;
            checkbox.style.flexShrink     = 0;
            checkbox.style.color          = isSelected
                ? new StyleColor(new Color(0.23f, 0.51f, 0.96f))
                : new StyleColor(new Color(0.58f, 0.64f, 0.72f));
            checkbox.pickingMode = PickingMode.Ignore;

            // Info container
            var info = new VisualElement();
            info.style.flexGrow      = 1;
            info.style.flexDirection = FlexDirection.Column;
            info.pickingMode         = PickingMode.Ignore;

            // Hàng: từ + rank badge
            var wordLine = new VisualElement();
            wordLine.style.flexDirection = FlexDirection.Row;
            wordLine.style.alignItems    = Align.Center;
            wordLine.pickingMode         = PickingMode.Ignore;

            var lblWord = new Label(word.word);
            lblWord.style.fontSize                = 26;
            lblWord.style.unityFontStyleAndWeight = FontStyle.Bold;
            lblWord.style.color                   = new StyleColor(Color.white);
            lblWord.style.marginRight             = 12;
            lblWord.pickingMode                   = PickingMode.Ignore;

            var lblRank = new Label(word.rankRequired ?? "Dong");
            lblRank.style.fontSize      = 15;
            lblRank.style.unityFontStyleAndWeight = FontStyle.Bold;
            lblRank.style.borderTopLeftRadius     = 10; lblRank.style.borderTopRightRadius    = 10;
            lblRank.style.borderBottomLeftRadius  = 10; lblRank.style.borderBottomRightRadius = 10;
            lblRank.style.paddingLeft   = 8; lblRank.style.paddingRight  = 8;
            lblRank.style.paddingTop    = 2; lblRank.style.paddingBottom = 2;
            lblRank.pickingMode                   = PickingMode.Ignore;

            string rankStr = word.rankRequired ?? "Dong";
            Color tierColor = new Color(0.8f, 0.5f, 0.2f); // Default Bronze/Orange
            if (rankStr.Equals("Bac", System.StringComparison.OrdinalIgnoreCase)) tierColor = new Color(0.75f, 0.75f, 0.75f);
            else if (rankStr.Equals("Vang", System.StringComparison.OrdinalIgnoreCase)) tierColor = new Color(1.0f, 0.84f, 0.0f);
            else if (rankStr.Equals("BachKim", System.StringComparison.OrdinalIgnoreCase)) tierColor = new Color(0.6f, 0.4f, 0.8f);
            else if (rankStr.Equals("KimCuong", System.StringComparison.OrdinalIgnoreCase)) tierColor = new Color(0.12f, 0.8f, 0.98f);
            else if (rankStr.Equals("SieuCap", System.StringComparison.OrdinalIgnoreCase)) tierColor = new Color(0.93f, 0.26f, 0.26f);

            lblRank.style.color = new StyleColor(tierColor);
            lblRank.style.backgroundColor = new StyleColor(new Color(tierColor.r, tierColor.g, tierColor.b, 0.12f));
            lblRank.style.borderTopColor = new StyleColor(new Color(tierColor.r, tierColor.g, tierColor.b, 0.4f));
            lblRank.style.borderBottomColor = new StyleColor(new Color(tierColor.r, tierColor.g, tierColor.b, 0.4f));
            lblRank.style.borderLeftColor = new StyleColor(new Color(tierColor.r, tierColor.g, tierColor.b, 0.4f));
            lblRank.style.borderRightColor = new StyleColor(new Color(tierColor.r, tierColor.g, tierColor.b, 0.4f));
            lblRank.style.borderTopWidth = 1.5f;
            lblRank.style.borderBottomWidth = 1.5f;
            lblRank.style.borderLeftWidth = 1.5f;
            lblRank.style.borderRightWidth = 1.5f;

            wordLine.Add(lblWord); wordLine.Add(lblRank);

            var lblMeaning = new Label(word.meaning);
            lblMeaning.style.fontSize = 21;
            lblMeaning.style.color    = new StyleColor(new Color(0.70f, 0.76f, 0.85f));
            lblMeaning.style.marginTop= 4;
            lblMeaning.pickingMode    = PickingMode.Ignore;

            var lblId = new Label($"ID: {word.id}");
            lblId.style.fontSize = 17;
            lblId.style.color    = new StyleColor(new Color(0.58f, 0.64f, 0.72f));
            lblId.pickingMode    = PickingMode.Ignore;

            info.Add(wordLine); info.Add(lblMeaning); info.Add(lblId);
            row.Add(checkbox); row.Add(info);

            // Click handler trực tiếp trên Button — không bị chặn
            row.clicked += () =>
            {
                if (_pickerSelected.Contains(word.id)) _pickerSelected.Remove(word.id);
                else _pickerSelected.Add(word.id);
                RefreshPickerList();
            };

            return row;
        }

        // Áp style màu sắc row theo trạng thái selected
        private void ApplyPickerRowStyle(Button row, bool isSelected)
        {
            row.style.backgroundColor = isSelected
                ? new StyleColor(new Color(0.23f, 0.51f, 0.96f, 0.15f))
                : new StyleColor(new Color(0.12f, 0.16f, 0.23f));
            var borderColor = isSelected
                ? new StyleColor(new Color(0.23f, 0.51f, 0.96f))
                : new StyleColor(new Color(0.20f, 0.25f, 0.33f));
            row.style.borderTopColor    = borderColor; row.style.borderBottomColor = borderColor;
            row.style.borderLeftColor   = borderColor; row.style.borderRightColor  = borderColor;
        }

        private void UpdatePickerCountLabels()
        {
            int cnt   = _pickerSelected.Count;
            string txt = cnt == 0 ? "Chưa có từ nào được chọn" : $"Đã chọn {cnt} từ";
            SetTextValue("picker-selected-subtitle", txt);
            SetTextValue("picker-footer-count", $"{cnt} từ được chọn");
        }

        //   PICKER PREVIEW — chip trong form
        private void RefreshPickerPreview()
        {
            var preview = _root.Q<VisualElement>("picker-preview");
            if (preview == null) return;
            preview.Clear();

            int cnt = _pickerSelected.Count;
            var badge = _root.Q<Label>("picker-count-badge");
            if (badge != null) badge.text = $"{cnt} từ đã chọn";

            if (cnt == 0)
            {
                var empty = new Label("Chưa chọn từ nào — bấm ＋ Chọn từ để thêm");
                empty.style.fontSize               = 18;
                empty.style.color                  = new StyleColor(new Color(0.70f, 0.68f, 0.65f));
                empty.style.unityFontStyleAndWeight= FontStyle.Italic;
                empty.style.alignSelf              = Align.Center;
                preview.Add(empty);
                return;
            }

            preview.style.flexDirection = FlexDirection.Column;
            preview.style.alignItems = Align.Stretch;

            var easyIds = new List<string>();
            var mediumIds = new List<string>();
            var hardIds = new List<string>();

            foreach (var id in _pickerSelected)
            {
                var word = _jsonDb?.words?.FirstOrDefault(w => w.id == id);
                string rankKey = word != null && !string.IsNullOrEmpty(word.rankRequired) ? word.rankRequired.Trim().ToLower() : "dong";
                
                if (rankKey == "dong" || rankKey == "bac")
                    easyIds.Add(id);
                else if (rankKey == "vang" || rankKey == "bachkim")
                    mediumIds.Add(id);
                else if (rankKey == "kimcuong" || rankKey == "sieucap")
                    hardIds.Add(id);
                else
                    easyIds.Add(id);
            }

            System.Action<string, List<string>, Color> addSection = (sectionName, ids, themeColor) =>
            {
                if (ids.Count == 0) return;

                var section = new VisualElement();
                section.style.flexDirection = FlexDirection.Column;
                section.style.marginBottom = 10;

                var headerRow = new VisualElement();
                headerRow.style.flexDirection = FlexDirection.Row;
                headerRow.style.alignItems = Align.Center;
                headerRow.style.marginBottom = 6;

                var indicator = new VisualElement();
                indicator.style.width = 10;
                indicator.style.height = 10;
                indicator.style.backgroundColor = new StyleColor(themeColor);
                indicator.style.borderTopLeftRadius = 5;
                indicator.style.borderTopRightRadius = 5;
                indicator.style.borderBottomLeftRadius = 5;
                indicator.style.borderBottomRightRadius = 5;
                indicator.style.marginRight = 8;

                var title = new Label($"{sectionName} ({ids.Count} từ)");
                title.style.fontSize = 14;
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                title.style.color = new StyleColor(new Color(0.8f, 0.8f, 0.8f));

                headerRow.Add(indicator);
                headerRow.Add(title);
                section.Add(headerRow);

                var chipsRow = new VisualElement();
                chipsRow.style.flexDirection = FlexDirection.Row;
                chipsRow.style.flexWrap = Wrap.Wrap;
                chipsRow.style.paddingLeft = 18;

                foreach (var id in ids.OrderBy(x => x))
                {
                    var word = _jsonDb?.words?.FirstOrDefault(w => w.id == id);
                    string lbl = word != null ? word.word : id;

                    var chip = new VisualElement();
                    chip.style.flexDirection = FlexDirection.Row;
                    chip.style.alignItems = Align.Center;
                    chip.style.backgroundColor = new StyleColor(new Color(0.12f, 0.16f, 0.23f));
                    chip.style.borderTopLeftRadius = 14;
                    chip.style.borderTopRightRadius = 14;
                    chip.style.borderBottomLeftRadius = 14;
                    chip.style.borderBottomRightRadius = 14;
                    chip.style.paddingLeft = 10;
                    chip.style.paddingRight = 6;
                    chip.style.paddingTop = 4;
                    chip.style.paddingBottom = 4;
                    chip.style.marginRight = 6;
                    chip.style.marginBottom = 6;
                    chip.style.borderTopWidth = 1;
                    chip.style.borderBottomWidth = 1;
                    chip.style.borderLeftWidth = 1;
                    chip.style.borderRightWidth = 1;
                    chip.style.borderTopColor = new StyleColor(new Color(themeColor.r, themeColor.g, themeColor.b, 0.4f));
                    chip.style.borderRightColor = new StyleColor(new Color(themeColor.r, themeColor.g, themeColor.b, 0.4f));
                    chip.style.borderBottomColor = new StyleColor(new Color(themeColor.r, themeColor.g, themeColor.b, 0.4f));
                    chip.style.borderLeftColor = new StyleColor(new Color(themeColor.r, themeColor.g, themeColor.b, 0.4f));

                    var chipLbl = new Label(lbl);
                    chipLbl.style.fontSize = 13;
                    chipLbl.style.color = new StyleColor(themeColor);
                    chipLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                    chipLbl.pickingMode = PickingMode.Ignore;

                    string capturedId = id;
                    var chipRemove = new Button();
                    chipRemove.text = "✕";
                    chipRemove.style.fontSize = 12;
                    chipRemove.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
                    chipRemove.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0));
                    chipRemove.style.borderTopWidth = 0;
                    chipRemove.style.borderBottomWidth = 0;
                    chipRemove.style.borderLeftWidth = 0;
                    chipRemove.style.borderRightWidth = 0;
                    chipRemove.style.marginLeft = 4;
                    chipRemove.clicked += () => { _pickerSelected.Remove(capturedId); RefreshPickerPreview(); };

                    chip.Add(chipLbl);
                    chip.Add(chipRemove);
                    chipsRow.Add(chip);
                }

                section.Add(chipsRow);
                preview.Add(section);
            };

            addSection("Dễ (Easy)", easyIds, new Color(0.10f, 0.73f, 0.51f));
            addSection("Trung Bình (Medium)", mediumIds, new Color(0.23f, 0.51f, 0.96f));
            addSection("Khó (Hard)", hardIds, new Color(0.93f, 0.26f, 0.26f));
        }

        // ═══════════════════════════════════════════
        //   DROPDOWN HELPERS
        // ═══════════════════════════════════════════
        private void SetDropdownChoices(string fieldName, List<string> choices)
        {
            var dd = _root.Q<DropdownField>(fieldName);
            if (dd == null) return;
            dd.choices = choices;
            if (choices.Count > 0 && !choices.Contains(dd.value)) dd.value = choices[0];
        }

        private void SetDropdownValue(string fieldName, string value)
        {
            var dd = _root.Q<DropdownField>(fieldName);
            if (dd != null && dd.choices != null && dd.choices.Contains(value)) dd.value = value;
        }

        private string GetDropdownValue(string fieldName)
        {
            return _root.Q<DropdownField>(fieldName)?.value ?? "";
        }

        // ═══════════════════════════════════════════
        //   ID GENERATION
        // ═══════════════════════════════════════════
        private string GenerateNextSetId()
        {
            if (_jsonDb.vocabSets == null || _jsonDb.vocabSets.Count == 0) return "vs1";
            int max = 0;
            foreach (var s in _jsonDb.vocabSets)
                if (s.id != null && s.id.StartsWith("vs") &&
                    int.TryParse(s.id.Substring(2), out int n) && n > max) max = n;
            return $"vs{max + 1}";
        }

        // ═══════════════════════════════════════════
        //   VISUAL HELPERS
        // ═══════════════════════════════════════════
        private string GetSetEmoji(string category) =>
            category.ToLower() switch
            {
                "nature"        => "🐾", "food"       => "🍎", "travel"   => "✈️",
                "technology"    => "💻", "sports"     => "⚽", "daily life"=> "🏠",
                "business"      => "💼", "education"  => "📚", "health"   => "💊",
                "entertainment" => "🎮", _            => "📖"
            };

        private UnityEngine.Color GetSetThumbnailColor(string category) =>
            category.ToLower() switch
            {
                "nature"       => new UnityEngine.Color(0.84f, 0.94f, 0.84f),
                "food"         => new UnityEngine.Color(1.00f, 0.93f, 0.84f),
                "travel"       => new UnityEngine.Color(0.84f, 0.91f, 1.00f),
                "technology"   => new UnityEngine.Color(0.84f, 0.84f, 0.96f),
                "sports"       => new UnityEngine.Color(1.00f, 0.84f, 0.84f),
                "daily life"   => new UnityEngine.Color(0.96f, 0.96f, 0.84f),
                "business"     => new UnityEngine.Color(0.88f, 0.84f, 0.96f),
                "education"    => new UnityEngine.Color(0.84f, 0.96f, 0.96f),
                _              => new UnityEngine.Color(0.94f, 0.94f, 0.94f)
            };

        private string GetDifficultyLabel(string difficulty) =>
            difficulty.ToLower() switch
            {
                "easy" => "Dễ", "medium" => "Trung Bình", "hard" => "Khó", "multi-level" => "Đa Cấp Độ", _ => difficulty
            };

        private string ParseDifficultyLabel(string label) =>
            label switch
            {
                "Dễ" => "Easy", "Trung Bình" => "Medium", "Khó" => "Hard", "Đa Cấp Độ" => "Multi-Level", _ => label
            };
    }
}
