using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Cysharp.Threading.Tasks;

namespace Tara.WaterSlide
{
    public class UI_GameView : UIViewBase
    {
        public TMP_Text levelText;
        public TMP_Text timeText;
        public Image timeBar;
        public Button retryButton;
        public Button pauseButton;

        [Header("Ice Effects")]
        public GameObject iceGroup;
        public Image iceImage; // 얼음 텍스처
        public Image iceTimeBar; // 얼음 타이머 바
        public Color iceTimeTextColor = Color.cyan;
        private float iceTotalDuration = 0f;
        private bool isFreezeActive = false;
        private Tween blinkTween;
        private bool hasShownOneMinuteWarning = false;
        private bool isWarningBlinking = false;
        private Color originalTimeTextColor = Color.white;

        public void SetUp()
        {
            GameManager.Instance.OnTimeChanged += UpdateTimeText;
            GameManager.Instance.OnTimerPauseStateChanged += OnFreezeStateChanged;

            Debug.Log($"UI_GameView Initialized {GameManager.Instance.CurrentLevel}");

            UpdateLevelText(GameManager.Instance.CurrentLevel);
            UpdateTimeText(GameManager.Instance.RemainingTimeSeconds);

            iceGroup.gameObject.SetActive(false);

            hasShownOneMinuteWarning = false;
            isWarningBlinking = false;
            originalTimeTextColor = timeText.color;
        }

        protected override void OnInit()
        {
            base.OnInit();
            retryButton.SetOnClickListener(OnRetryButtonClick);

            if (pauseButton != null)
            {
                if (pauseButton != null)
                {
                    pauseButton.SetOnClickListener(OnPauseButtonClick);
                }
            }

            GameManager.Instance.OnTimeChanged += UpdateTimeText;
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;

            UpdateLevelText(GameManager.Instance.CurrentLevel);
            UpdateTimeText(GameManager.Instance.RemainingTimeSeconds);
            UpdateRetryButtonState();
        }

        protected override void Update()
        {
            if (isFreezeActive)
            {
                float remain = GameManager.Instance.ManualFreezeRemain;

                if (remain <= 0f)
                    return;

                iceTimeBar.fillAmount = remain / iceTotalDuration;
            }
        }

        public void UpdateLevelText(int level)
        {
            levelText.text = $"Level {level}";
        }

        public void UpdateTimeText(int time)
        {
            timeText.text = $"{time / 60:00}:{time % 60:00}";
            timeBar.fillAmount = (float)time / GameManager.Instance.TimeLimit;
            CheckTimeWarnings(time);
        }

        private void CheckTimeWarnings(int remainingTime)
        {
            if (remainingTime == 60 && !hasShownOneMinuteWarning)
            {
                hasShownOneMinuteWarning = true;
                StartOneMinuteWarningBlink().Forget();
            }
            else if (remainingTime < 15 && remainingTime > 0)
            {
                if (!isWarningBlinking)
                    StartContinuousWarningBlink();
            }
            else if (remainingTime >= 15 && isWarningBlinking)
            {
                StopTimeBlinking();
            }
        }

        private async UniTaskVoid StartOneMinuteWarningBlink()
        {
            // 1회 깜빡임
            timeText.color = Color.red;
            await timeText.DOFade(0.2f, 0.5f).SetLoops(2, LoopType.Yoyo).AsyncWaitForCompletion();
            timeText.color = originalTimeTextColor;
        }

        private void StartContinuousWarningBlink()
        {
            isWarningBlinking = true;
            timeText.color = Color.red;

            // 지속 깜빡임 (얼음 효과와 겹치지 않게 blinkTween 재사용)
            blinkTween?.Kill();
            blinkTween = timeText.DOFade(0.2f, 0.4f).SetLoops(-1, LoopType.Yoyo);
        }

        private void StopTimeBlinking()
        {
            isWarningBlinking = false;
            blinkTween?.Kill();
            timeText.DOFade(1f, 0.1f);
            timeText.color = originalTimeTextColor;
        }

        private void OnGameStateChanged()
        {
            UpdateRetryButtonState();
        }

        private void OnRetryButtonClick()
        {
            // 게임 재시작 로직 (필요에 따라 구현)
            Debug.Log("Retry Button Clicked");
            if (SceneSystemManager.Instance != null)
            {
                SceneSystemManager.Instance.LoadScene(SceneLib.GAME);
            }
            // 예: SceneSystemManager.Instance.LoadScene(SceneLib.GAME);
        }

        private void UpdateRetryButtonState()
        {
            if (retryButton == null) return;

            // 예: 게임 오버나 클리어 상태일 때만 활성화하거나, 항상 활성화
            // 현재 게임 상태에 따라 버튼 활성/비활성 처리
            // bool canRetry = GameManager.Instance.currentGameState != GameState.Playing;
            // retryButton.interactable = canRetry;
        }

        private void OnPauseButtonClick()
        {
            var settingPopup = UISystemManager._GetView<UI_SettingPopup>();

            if (settingPopup != null)
            {
                settingPopup.Show();
            }
            else
            {
                Debug.LogError("[UI_GameView] UI_SettingPopup을 찾을 수 없습니다. 씬에 배치되어 있는지 확인하세요.");
            }
        }


        #region ICE ITEM EFFECTS
        private void OnFreezeStateChanged(bool isPaused)
        {
            if (isPaused)
                PlayFreezeUIEffects();
            else
                StopFreezeUIEffects();
        }

        private void PlayFreezeUIEffects()
        {
            isFreezeActive = true;
            StopTimeBlinking();
            iceGroup.gameObject.SetActive(true);

            // 얼음 지속 시간 저장(10초)
            iceTotalDuration = GameManager.Instance.ManualFreezeRemain;

            // 얼음 텍스처 Fade-In
            iceImage.color = new Color(iceImage.color.r, iceImage.color.g, iceImage.color.b, 0f);
            iceImage.DOFade(0.2f, 0.5f);

            // 전체 타이머 Text 색 변경
            timeText.color = iceTimeTextColor;

            // 전체 타이머 Text 깜빡임 효과
            blinkTween?.Kill();
            blinkTween = timeText.DOFade(0.2f, 0.4f).SetLoops(-1, LoopType.Yoyo);

            iceTimeBar.fillAmount = 1f;
        }

        private void StopFreezeUIEffects()
        {
            isFreezeActive = false;
            iceTimeBar.fillAmount = 0f;
            timeText.color = Color.white;

            blinkTween?.Kill();
            timeText.alpha = 1f;

            // 얼음 텍스처 Fade-Out
            iceImage.DOFade(0f, 0.5f).OnComplete(() => iceGroup.gameObject.SetActive(false));

            if (GameManager.Instance.RemainingTimeSeconds < 15 && GameManager.Instance.RemainingTimeSeconds > 0)
            {
                CheckTimeWarnings(GameManager.Instance.RemainingTimeSeconds);
            }
        }
        #endregion
    }
}