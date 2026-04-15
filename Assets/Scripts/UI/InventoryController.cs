using UnityEngine.UIElements;
using System;
using UnityEngine;
using Unity.VisualScripting.FullSerializer;
using System.Linq;
using NUnit.Framework;
using System.Runtime.InteropServices;

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
}