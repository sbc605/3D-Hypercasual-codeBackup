using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Tara.WaterSlide;
using UnityEngine;

public class ItemController : Singleton<ItemController>
{
    [SerializeField] private ItemConfigBase iceConfig;
    [SerializeField] private ItemConfigBase magnetConfig;
    [SerializeField] private ItemConfigBase helicopterConfig;

    [SerializeField] private ItemInventoryModel inventory;
    public bool HasUsedItem { get; private set; }

    // 현재 어떤 아이템이라도 사용(입력 대기 포함) 중인지 여부
    private bool _isAnyItemBusy = false;

    // 아이템 등록용 딕셔너리
    private Dictionary<ItemType, ItemBase> _items = new Dictionary<ItemType, ItemBase>();

    // 아이템 타입별 버튼 UI(RectTransform) 저장소
    private Dictionary<ItemType, RectTransform> _itemButtons = new Dictionary<ItemType, RectTransform>();

    #region First Settings
    protected override void OnInit()
    {
        base.OnInit();

        HasUsedItem = false;

        Register(new IceItem(iceConfig));
        Register(new MagnetItem(magnetConfig));
        Register(new HelicopterItem(helicopterConfig));
    }

    public override void OnCreateInstance()
    {
        base.OnCreateInstance();
    }
    #endregion

    #region Button

    /// <summary>
    /// 아이템과 UI를 연결하는 함수
    /// </summary>
    public void BindItemButton(ItemType type, RectTransform buttonRect)
    {
        if (!_itemButtons.ContainsKey(type))
        {
            _itemButtons.Add(type, buttonRect);
        }
        else
        {
            _itemButtons[type] = buttonRect;
        }
    }

    /// <summary>
    /// 저장된 버튼 참조를 조회하는 함수
    /// 버튼에 하이라이트, 버튼 잠금 애니메이션, 아이템 실패 시 버튼 흔들림 같은 기능을 구현할 거면 해당 함수가 필요함.
    /// </summary>
    public RectTransform GetItemButton(ItemType type)
    {
        if (_itemButtons.TryGetValue(type, out var rect))
        {
            return rect;
        }
        return null;
    }

    public ItemConfigBase GetConfig(ItemType type)
    {
        return type switch
        {
            ItemType.ICE => iceConfig,
            ItemType.HELICOPTER => helicopterConfig,
            ItemType.MAGNET => magnetConfig,
            _ => null
        };
    }

    // 아이템 타입과 아이템을 딕셔너리에 등록
    public void Register(ItemBase item)
    {
        if (item == null)
            return;

        ItemType type = item.Config.itemType;

        if (!_items.ContainsKey(type))
        {
            _items.Add(type, item);
            Debug.Log($"{type} 아이템 등록 완료");
        }
    }
    #endregion

    #region 아이템 사용
    public bool UseItem(ItemType type)
    {
        if (!_items.TryGetValue(type, out var item))
            return false;

        // 개별 아이템의 CanUse 뿐만 아니라 Controller의 전역 상태도 체크
        if (_isAnyItemBusy || !item.CanUse())
        {
            return false;
        }

        if (!item.CanUse())
            return false;

        HasUsedItem = true;
        _isAnyItemBusy = true;

        UseItemInternal(type, item).Forget();
        return true;
    }

    private async UniTaskVoid UseItemInternal(ItemType type, ItemBase item)
    {
        try
        {
            bool success = await item.UseAsync();

            if (success)
                inventory.ConsumeItems(type, 1);
        }
        finally
        {
            // 아이템 사용이 끝났거나(success), 취소되었거나(fail), 
            // 예외가 발생해도 반드시 플래그를 해제하여 다른 아이템을 쓸 수 있게 함
            _isAnyItemBusy = false;
        }
    }

    // 인벤토리 변경 이벤트
    public event Action OnInventoryChanged;

    #endregion
}