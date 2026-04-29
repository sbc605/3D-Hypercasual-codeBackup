using UnityEngine;
using AppsInToss;
using System;
using Tara.WaterSlide;
using System.Collections.Generic;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    [Header("Ad Group IDs (Toss Console)")]
    [SerializeField] private string interstitialAdGroupId;
    [SerializeField] private string rewardedAdGroupId;

    private bool _isInterstitialLoaded;
    private bool _isRewardedLoaded;

    private Action _interstitialLoadDispose;
    private Action _rewardedLoadDispose;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        PreloadAds();
    }

    #region 광고 사전 로드
    public void PreloadAds()
    {
        LoadInterstitial();
        LoadRewarded();
    }

    private void LoadInterstitial()
    {
        _interstitialLoadDispose?.Invoke();
        _isInterstitialLoaded = false;

        LogAd("Interstitial", $"Load requested (AdGroupId={interstitialAdGroupId})");

#pragma warning disable CS0618
        _interstitialLoadDispose = AIT.GoogleAdMobLoadAppsInTossAdMob(
            onEvent: (evt) =>
            {
                LogAd("Interstitial", $"Load event: {evt.Type}");

                if (evt.Type == "loaded")
                {
                    _isInterstitialLoaded = true;
                    LogAd("Interstitial", "Loaded successfully");
                }
            },
            options: new LoadAdMobOptions { AdGroupId = interstitialAdGroupId },
            onError: (err) =>
            {
                LogAd("Interstitial", $"Load error: {err.ErrorCode} - {err.Message}");
            }
        );
#pragma warning restore CS0618
    }

    private void LoadRewarded()
    {
        _rewardedLoadDispose?.Invoke();
        _isRewardedLoaded = false;

        LogAd("Rewarded", $"Load requested (AdGroupId={rewardedAdGroupId})");

#pragma warning disable CS0618
        _rewardedLoadDispose = AIT.GoogleAdMobLoadAppsInTossAdMob(
            onEvent: (evt) =>
            {
                LogAd("Rewarded", $"Load event: {evt.Type}");

                if (evt.Type == "loaded")
                {
                    _isRewardedLoaded = true;
                    LogAd("Rewarded", "Loaded successfully");
                }
            },
            options: new LoadAdMobOptions { AdGroupId = rewardedAdGroupId },
            onError: (err) =>
            {
                LogAd("Rewarded", $"Load error: {err.ErrorCode} - {err.Message}");
            }
        );
#pragma warning restore CS0618
    }
    #endregion

    #region 전면 광고 표시
    public void ShowInterstitial(Action onClosed = null)
    {

        if (!_isInterstitialLoaded)
        {
            LogAd("Interstitial", "Show called but NOT loaded");
            onClosed?.Invoke();
            return;
        }

        LogAd("Interstitial", "Show requested");

#pragma warning disable CS0618

        bool adShowTracked = false;

        AIT.GoogleAdMobShowAppsInTossAdMob(
            onEvent: (evt) =>
            {
                if (!adShowTracked && (evt.Type == "show" || evt.Type == "impression"))
                {
                    adShowTracked = true;

                    WebGLBridge.Instance?.TrackEvent(
                        "ad_show",
                        new Dictionary<string, object>
                        {
                { "ad_type", "interstitial" },
                { "platform", "webgl" },
                { "provider", "toss" }
                        }
                    );
                }

                LogAd("Interstitial", $"Show event: {evt.Type}");

                if (evt.Type == "dismissed")
                {
                    _isInterstitialLoaded = false;
                    LoadInterstitial();
                    onClosed?.Invoke();
                }
            },
            options: new ShowAdMobOptions { AdGroupId = interstitialAdGroupId },
            onError: (err) =>
            {
                LogAd("Interstitial", $"Show error: {err.ErrorCode} - {err.Message}");
                onClosed?.Invoke();
            }
        );
#pragma warning restore CS0618
    }
    #endregion

    #region 보상형 광고
    /// <summary>
    /// 보상 지급은 userEarnedReward에서
    /// 광고가 닫힐 때 보상 여부 onFinished로 전달
    /// </summary>
    public void ShowRewarded(Action<bool> onFinished)
    {
        if (!_isRewardedLoaded)
        {
            Debug.Log("[AdManager] Rewarded not ready");

             WebGLBridge.Instance?.TrackEvent(
        "ad_fail",
        new Dictionary<string, object>
        {
            { "ad_type", "rewarded" },
            { "reason", "not_loaded" },
            { "platform", "webgl" },
            { "provider", "toss" }
        }
    );
    
            onFinished?.Invoke(false);
            return;
        }

#pragma warning disable CS0618
        bool rewarded = false;
        bool adShowTracked = false; // WebGL은 SDK 이벤트 기반으로 판단

        AIT.GoogleAdMobShowAppsInTossAdMob(
            onEvent: (evt) =>
            {
                if (!adShowTracked && (evt.Type == "show" || evt.Type == "impression"))
                {
                    adShowTracked = true;

                    WebGLBridge.Instance?.TrackEvent(
                        "ad_show",
                        new Dictionary<string, object>
                        {
                { "ad_type", "rewarded" },
                { "platform", "webgl" },
                { "provider", "toss" }
                        }
                    );
                }

                if (evt.Type == "userEarnedReward")
                {
                    rewarded = true;
                }
                else if (evt.Type == "dismissed")
                {
                    _isRewardedLoaded = false;
                    LoadRewarded();

                    WebGLBridge.Instance?.TrackEvent(
        "ad_complete",
        new Dictionary<string, object>
        {
            { "ad_type", "rewarded" },
            { "rewarded", rewarded },
            { "platform", "webgl" },
            { "provider", "toss" }
        }
    );

                    onFinished?.Invoke(rewarded);
                }
            },
            options: new ShowAdMobOptions { AdGroupId = rewardedAdGroupId },
            onError: (err) =>
            {
                Debug.LogWarning($"[AdManager] Rewarded show error: {err.ErrorCode} - {err.Message}");

                 WebGLBridge.Instance?.TrackEvent(
        "ad_fail",
        new Dictionary<string, object>
        {
            { "ad_type", "rewarded" },
            { "reason", err.ErrorCode },
            { "platform", "webgl" },
            { "provider", "toss" }
        }
    );
    
                onFinished?.Invoke(false);
            }
        );
#pragma warning restore CS0618
    }
    #endregion

    /// <summary>
    /// 공통 로그 함수
    /// </summary>
    private void LogAd(string tag, string message)
    {
        Debug.Log($"[AdManager][{tag}] {message}");
    }
}
