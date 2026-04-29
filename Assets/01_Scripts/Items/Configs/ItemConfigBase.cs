using System.Collections.Generic;
using Tara.WaterSlide;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemConfigBase", menuName = "ItemConfigBase/ItemConfigBase")]
public class ItemConfigBase : ScriptableObject
{
    public ItemType itemType;
    public Sprite icon;
    public float cooldownSec;
    public AudioClip sfx;
    public LocalizedString description;

    [Header("Extra SFX (Optional)")]
    // 추가 효과음들을 담을 리스트
    public List<AudioClip> extraSfx;
}
