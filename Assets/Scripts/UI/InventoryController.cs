using UnityEngine.UIElements;
using System;
using UnityEngine;
using Unity.VisualScripting.FullSerializer;
using System.Linq;
using NUnit.Framework;
using System.Runtime.InteropServices;
using System.ComponentModel;
using JetBrains.Annotations;
using VocabLearning.Data;
using Unity.VisualScripting.ReorderableList.Element_Adder_Menu;
using Mono.Cecil;

public class InventoryController
{
    private VisualElement _root;
    private Action<VisualTreeAsset> _onNavigate;
    private VocabLearning.Data.MockDatabase _db;

    public InventoryController(VisualElement root, VocabLearning.Data.MockDatabase db, Action<VisualTreeAsset> onNavigate)
    {
        _root = root;
        _db = db;
        _onNavigate = onNavigate;
    }

    public void Bind(VisualTreeAsset profileAsset)
    {
        _root.Q<Button>("btnBack")?.RegisterCallback<ClickEvent>(evt => _onNavigate?.Invoke(profileAsset));

    }

    public void LoadInventory(VisualTreeAsset InventoryItemTemplate)
    {  
        var user =_db.currentUser;
        if(user == null) return;

        var lblTotal = _root.Q<Label>("Lbl_TotalOwner");
        if(lblTotal != null)
        { 
            int totalItems = 0;

          if (_db.userItems != null)
          {
              totalItems = _db.userItems
                  .Count(x => x.userId == user.id);
          }
       Debug.Log("Total items of this user: " + totalItems);
       lblTotal.text = $"{totalItems} items owned";
        }
  

        var cardGrid = _root.Q<VisualElement>("CardGrid");
        if (cardGrid == null || InventoryItemTemplate == null)
        {
            Debug.LogError("Không tìm thấy CardGrid hoặc chưa kéo ItemTemplate!");
            return;
        }
        cardGrid.Clear();

        string currentUserId = _db.currentUser.id.ToString();

        var ownedItemIds = _db.userItems.Where(ui => ui.userId == currentUserId && ui.quantity >0)
                                        .Select(ui => ui.itemId)
                                        .ToList();
        var displayItems = _db.items.Where(item => ownedItemIds.Contains(item.itemId)).ToList();   

        foreach (var item in displayItems)
        {
            VisualElement card = InventoryItemTemplate.Instantiate();
            BindItem(card, item);
            card.Q<Button>("btnEquip")?.RegisterCallback<ClickEvent>(evt => {
             _db.currentUser.avatar = item.imageUrl;
             Debug.Log($"Đã trang bị: {item.name}");
             LoadInventory(InventoryItemTemplate);
        });
           cardGrid.Add(card);
        }



    }

    public void BindItem(VisualElement e, ItemData item)
     {
       e.Q<Label>("Lbl_name").text = item.name;

       //load anh
       var img = e.Q<VisualElement>("Img_logo");
       var text = Resources.Load<Texture2D>("Sprites/Icon/"+item.imageUrl.Replace(".png",""));
        if (text != null)
        {
            img.style.backgroundImage = new StyleBackground(text);
        }
        else
        {
              Debug.LogError("Không load được ảnh: " + item.imageUrl);
        }
        
        var btnEquip = e.Q<Button>("btnEquip");
        bool isEquipped = (_db.currentUser.avatar == item.imageUrl);
        
        if (isEquipped)
        {
            btnEquip.text ="Used";
            btnEquip.SetEnabled(false);
            btnEquip.AddToClassList("btn-equipped-active");
        }
        else
        {
            btnEquip.text = "Equip";
            btnEquip.SetEnabled(true);
            btnEquip.RemoveFromClassList("btn-equipped-active");
        }

        
    }
} 