using UnityEngine;
using Cysharp.Threading.Tasks;

public abstract class ItemBase
{
    public abstract ItemConfigBase Config { get; }

    public abstract bool IsOnCooldown { get; }

    public abstract bool CanUse();

    /// <summary>
    /// true = 성공 → 소비
    /// false = 실패 → 미소비
    /// </summary>
    public abstract UniTask<bool> UseAsync();

    protected void PlaySFX()
    {
        if (Config?.sfx == null)
            return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(Config.sfx);
        }
        else
        {
            Debug.LogWarning("SoundManager가 씬에 없음");
        }
    }

    protected void PlayExtraSFX(int index)
    {
        if (Config != null &&
            Config.extraSfx != null &&
            index >= 0 &&
            index < Config.extraSfx.Count)
        {
            var clip = Config.extraSfx[index];

            if (clip != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(clip);
            }
        }
    }
}
