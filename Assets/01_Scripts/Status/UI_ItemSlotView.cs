using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Tara.WaterSlide;

public class UI_ItemSlotView : UIViewBase
{
    [Serializable]
    public class ItemSlot
    {
        public Button button;
        public Image icon;
        public TMP_Text amount;
    }

    [Header("Item Slots")]
    [SerializeField] private ItemSlot ice;
    [SerializeField] private ItemSlot helicopter;
    [SerializeField] private ItemSlot magnet;

    private Dictionary<ItemType, ItemSlot> _slots;
    [SerializeField] private ItemInventoryModel inventory;

    [SerializeField] private Color disabledColor = Color.gray;
    private Color _originColor;

    private bool _initialized = false;

    private void OnEnable()
    {
        if (!_initialized)
        {
            SetUp();
            _initialized = true;
        }
    }

    public void SetUp()
    {
        var controller = ItemController.Instance;
        _slots = new Dictionary<ItemType, ItemSlot>
        {
            { ItemType.ICE, ice },
            { ItemType.HELICOPTER, helicopter },
            { ItemType.MAGNET, magnet }
        };

        _originColor = ice.icon.color;

        // 아이콘 적용 + 클릭 이벤트
        foreach (var s in _slots)
        {
            var type = s.Key;
            var slot = s.Value;

            slot.icon.sprite = controller.GetConfig(type).icon;
            slot.button.onClick.RemoveAllListeners(); // 중복 방지
            slot.button.onClick.AddListener(() => controller.UseItem(type));

            controller.BindItemButton(type, slot.button.GetComponent<RectTransform>());
        }

        // 인벤토리 이벤트 연결
        inventory.OnItemCountChanged += OnInventoryChanged;

        // 최초 UI 동기화
        foreach (var type in _slots.Keys)
        {
            UpdateItemCountState(type, inventory.ReturnCurrentItemsCount(type));
        }
    }

    #region 아이템 재고 관련
    private void OnInventoryChanged(ItemType type, int count)
    {
        UpdateItemCountState(type, count);
    }

    /// <summary>
    /// 아이템 보유 개수를 UI 텍스트에 표시
    /// </summary>
    private void UpdateItemCountState(ItemType type, int count)
    {
        if (!_slots.TryGetValue(type, out var slot))
            return;

        slot.amount.text = count.ToString();

        bool enabled = count > 0;
        slot.button.interactable = enabled;
        slot.icon.color = enabled ? _originColor : disabledColor;
    }
    #endregion
}
