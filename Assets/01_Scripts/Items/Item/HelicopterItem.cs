using Tara.WaterSlide;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class HelicopterItem : ItemBase, IInputOverride
{
    private bool _isUsing = false;
    private UniTaskCompletionSource<bool> _inputSource;

    public override ItemConfigBase Config { get; }

    public HelicopterItem(ItemConfigBase config)
    {
        Config = config;
    }

    public override bool IsOnCooldown => false;

    public override bool CanUse()
    {
        return !_isUsing;
    }

    public override async UniTask<bool> UseAsync()
    {
        if (!CanUse()) return false;

        _isUsing = true;

        RectTransform myBtnRect = ItemController.Instance.GetItemButton(Config.itemType);

        // 하이라이트 시작(Duck 타입만 강조)
        HighlightController.Instance.HighlightObjectsByType(TileEntityType.Duck, myBtnRect);

        InputManager.Instance.SetInputOverride(this);

        // 입력 대기
        _inputSource = new UniTaskCompletionSource<bool>();

        bool success = await _inputSource.Task;
        _isUsing = false;
        // 하이라이트 종료
        HighlightController.Instance.ClearHighlight();

        return success;
    }

    public bool OnTap(TileController tile)
    {
        if (tile == null || tile.Entity is not DuckEntity duck)
            return true;

        ExecuteHelicopter(tile, duck).Forget();
        End();

        _inputSource?.TrySetResult(true);
        return true;
    }

    public bool OnDragStart(TileController tile) => true;

    /// <summary>
    /// 다른 UI 클릭, 뒤로가기, 제한 시간 만료엔 End(false)로 불러 미소비 한다.
    /// </summary>
    private void End(bool success = true)
    {
        InputManager.Instance.SetInputOverride(null);
        _inputSource?.TrySetResult(success);
    }

    #region 헬리콥터 아이템 기능 구현

    private async UniTaskVoid ExecuteHelicopter(TileController duckController, DuckEntity duckEntity)
    {
        var levelData = GameManager.Instance.CurrentLevelData;
        if (levelData == null)
        {
            Debug.LogError("[HelicopterItem] LevelData null");
            return;
        }

        var tileManager = TileManager.Instance;

        int duckJsonId = duckController.JsonTileId;
        var mappings = levelData.Mappings.Where(m => m.DuckTileId == duckJsonId).ToList();

        if (mappings.Count == 0) return;

        foreach (var mapping in mappings)
        {
            // Goal 찾기
            var goalController = tileManager.GetTileByJsonId(mapping.GoalTileId);
            if (goalController == null)
                continue;

            var goalEntity = goalController.Entity as GoalEntity;
            if (goalEntity == null) continue;

            // 연출
            var duckMesh = FindDuckMesh(duckController);
            if (duckMesh == null)
                return;

            Vector3 start = duckMesh.position;
            Vector3 peak = start + Vector3.up * 2f;
            Vector3 goalPos = goalController.transform.position + Vector3.up * 2f;

            await Move(duckMesh, start, peak, 0.2f); //두더지 위로 이동
            await Move(duckMesh, peak, goalPos, 1.8f); //두더지 골로 이동

            var goalEffect = goalController.GetComponentInChildren<GoalVisualObject>();

            // 두더지 골 들어가는 애니메이션
            var mole = duckMesh.GetComponent<MoleVisualObject>();
            await mole.MoleFall(goalEffect);

            await NewMethod();

            async UniTask NewMethod()
            {
                await UniTask.SwitchToMainThread(); // 안전하게 메인 스레드
                //Goal Count 처리
                goalEntity.AcceptDucksFrom(duckEntity);
                goalController.UpdateGoalDisplay();
            }

            // Goal 완료 시 두더지와 골 제거
            //await tileManager.RemoveTile(duckEntity.Id);

            if (goalEntity.IsCompleted)
            {
                await tileManager.RemoveTile(goalEntity.Id);
            }

            // mapping된 파이프 제거 [수정 : 최우석]
            var pipeRemoveTasks = new List<UniTask>();

            if (duckController != null)
            {
                pipeRemoveTasks.Add(SinkAndRemovePipe(duckController, tileManager));
            }

            foreach (int pipeJsonId in mapping.PipeTileIds)
            {
                Debug.Log($"[Helicopter] try pipeJsonId = {pipeJsonId}");

                var pipeController = tileManager.GetTileByJsonId(pipeJsonId);

                if (pipeController == null) continue;

                // 각 파이프에 대해 하강 및 제거 작업을 리스트에 추가
                pipeRemoveTasks.Add(SinkAndRemovePipe(pipeController, tileManager));
            }

            // 모든 파이프의 처리가 끝날 때까지 대기
            await UniTask.WhenAll(pipeRemoveTasks);

            GimmickManager.Instance.ApplyFrozenTickToItems("HelicopterItem", 1);
        }
    }

    private async UniTask SinkAndRemovePipe(TileController pipe, TileManager tileManager)
    {
        if (pipe == null || pipe.Entity == null) return;
        if ((pipe.Entity.Traits & PipeTrait.Permanent) != 0) return;

        // 하강시킬 대상(Transform)을 결정합니다.
        Transform targetTransform = null;

        if (pipe.TileType == TileType.Duck && pipe.DuckGroundObject != null)
        {
            targetTransform = pipe.DuckGroundObject.transform;
        }

        else
        {
            targetTransform = pipe.transform;
        }

        // 1. 하강 애니메이션 (targetTransform이 존재할 때만 실행)
        if (targetTransform != null)
        {
            Vector3 startPos = targetTransform.position;
            Vector3 endPos = startPos + Vector3.down * 1.5f; // 아래로 1.5만큼 이동

            float duration = 1.0f;
            float time = 0f;

            while (time < duration)
            {
                // 중간에 객체가 파괴되었는지 체크 (pipe나 target이 사라지면 중단)
                if (pipe == null || targetTransform == null) break;

                float t = time / duration;
                targetTransform.position = Vector3.Lerp(startPos, endPos, t);

                time += Time.deltaTime;
                await UniTask.Yield();
            }

            // 최종 위치 보정
            if (targetTransform != null)
                targetTransform.position = endPos;
        }

        // 2. TileManager를 통해 제거 (데이터 및 오브젝트 삭제)
        if (pipe != null && pipe.Entity != null)
        {
            await tileManager.RemoveTile(pipe.Entity.Id);
        }
    }

    /// <summary>
    /// Duck을 움직이기 위해 사용하는 함수
    /// </summary>
    private async UniTask Move(Transform t, Vector3 from, Vector3 to, float duration)
    {
        PlaySFX();

        float time = 0f;

        MoleVisualObject mole = t.GetComponentInChildren<MoleVisualObject>();
        mole.GrabMoleAnim();
        while (time < duration)
        {
            t.position = Vector3.Lerp(from, to, time / duration);
            var pos = t.transform.position;
            pos.y = 1.8f;
            t.transform.position = pos;
            time += Time.deltaTime;
            await UniTask.Yield();
        }
        //t.position = to; 포지션 덮어씌워지기 방지로 주석처리
    }

    private Transform FindDuckMesh(TileController controller)
    {
        foreach (var t in controller.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.Contains("Mesh_Duck"))
                return t;
        }
        return null;
    }
    #endregion
}