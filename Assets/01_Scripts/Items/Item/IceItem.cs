using UnityEngine;
using Cysharp.Threading.Tasks;
using Tara.WaterSlide;

public class IceItem : ItemBase
{
    private bool _isUsing = false;

    private float freezeDuration = 10f;
    public float PauseDuration => freezeDuration;

    public override ItemConfigBase Config { get; }

    public IceItem(ItemConfigBase config)
    {
        Config = config;
    }

    public override bool IsOnCooldown => false;

    public override bool CanUse() => !IsOnCooldown && !_isUsing;

    public override async UniTask<bool> UseAsync()
    {
        if (!CanUse())
            return false;

        _isUsing = true;

        PlaySFX();

        // 전역 타이머 정지 (비동기 효과)
        GameManager.Instance.PauseTimer(freezeDuration);

        try
        {
            // 지속 시간만큼 비동기 대기(초 단위를 밀리초로 변환)
            await UniTask.Delay((int)(freezeDuration * 1000));
        }
        finally
        {
            _isUsing = false;
        }

        return true;
    }

    /// <summary>
    /// Ice 효과 지속 중일 동안은 중복 사용 불가능
    /// </summary>
    private async UniTaskVoid ReleaseUsingAfterDelay()
    {
        await UniTask.Delay((int)(freezeDuration * 1000));
        _isUsing = false;
    }
}
