using UnityEngine;
using System;
using System.Collections.Generic;
using Tara.WaterSlide;

/// <summary>
/// 아이템 재고(수량)를 관리하는 클래스
/// </summary>
public class ItemInventoryModel : MonoBehaviour
{
    // 아이템 타입별 현재 보유 개수
    private Dictionary<ItemType, int> _counts = new();

    //특정 아이템 수량이 변경되었을 때 호출되는 이벤트(ItemType, 변경 후 수량)
    public event Action<ItemType, int> OnItemCountChanged;

    // 시작시 기본적으로 2개 주도록 제한하는 용도로 추가
    private void Awake()
    {
        SetItemCount(ItemType.ICE, 2);
        SetItemCount(ItemType.HELICOPTER, 2);
        SetItemCount(ItemType.MAGNET, 2);
    }

    /// <summary>
    /// 아이템 수량을 강제로 설정 (세이브 로드, 초기 지급, 디버그 용)
    /// </summary>
    public void SetItemCount(ItemType type, int count)
    {
        _counts[type] = Mathf.Max(0, count);
        OnItemCountChanged?.Invoke(type, _counts[type]);
    }

    /// <summary>
    /// 현재 아이템 수량 반환
    /// </summary>
    public int ReturnCurrentItemsCount(ItemType type)
    {
        return _counts.TryGetValue(type, out var count) ? count : 0;
    }

    /// <summary>
    /// 아이템 소비
    /// 보유 수량이 부족하면 false 반환
    /// </summary>
    public bool ConsumeItems(ItemType type, int amount = 1)
    {
        if (ReturnCurrentItemsCount(type) < amount)
            return false;

        _counts[type] -= amount;
        OnItemCountChanged?.Invoke(type, _counts[type]);
        return true;
    }

    /// <summary>
    /// 아이템 추가
    /// </summary>
    public void AddItems(ItemType type, int amount = 1)
    {
        if (amount <= 0) return;

        int current = ReturnCurrentItemsCount(type);
        _counts[type] = current + amount;

        OnItemCountChanged?.Invoke(type, _counts[type]);
    }
}
