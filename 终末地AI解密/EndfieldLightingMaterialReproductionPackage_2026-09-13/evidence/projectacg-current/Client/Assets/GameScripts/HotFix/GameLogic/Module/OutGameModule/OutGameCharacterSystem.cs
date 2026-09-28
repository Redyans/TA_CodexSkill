using Cysharp.Threading.Tasks;
using GameConfig;
using GameLogic.Battle;
using GameLogicService.User;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TEngine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameLogic
{
    /// <summary>
    /// 局外角色模型系统：运行时创建/销毁角色模型，并按场景上下文应用能力组合。
    /// </summary>
    internal class OutGameCharacterSystem
    {
        private readonly OutGameModule _module;
        private readonly OutGameCharacterPool _characterPool;
        private OutGameCharacterPool.Lease _characterLease;
        private const string CharacterRootName = "OutGameCharacterRoot";
        private const string WeaponTouchPivotName = "OutGameWeaponTouchPivot";
        private const string WeaponDisplayRootName = "OutGameWeaponDisplayRoot";
        private const string WeaponStagingRootName = "OutGameWeaponStagingRoot";
        private const string DetailPresentationStagingRootName = "OutGameDetailPresentationStagingRoot";
        private const string LegacyWeaponRootName = "OutGameWeaponRoot";
        private const string CharacterLayerName = "Characters";
        private const string WeaponLayerName = "Weapon";
        private const string DetailMainCameraName = "DetailCharacterCamera";
        private const string DetailWeaponCameraName = "DetailWeaponCamera";
        private const string DefaultHouseholdCharacterPrefabPath = "Assets/AssetRaw/Actor/Household/Household002.prefab";
        // 卧室默认开场希望角色坐在餐桌处；这里的对象名来自现有 HallInteraction / 场景交互对象配置。
        private const string HouseholdInitialDiningTableObjectName = "dining_table_chair";
        private static readonly int ShowLobbyIdle = Animator.StringToHash("ShowLobbyIdle");
        // 只把图中确认的餐桌稳定 idle 作为初始候选，避免开场随机到切换动作或 play 动作。
        private static readonly HashSet<string> HouseholdInitialDiningTableIdleNames = new HashSet<string>
        {
            "DiningTable_Idle0",
            "DiningTable_Idle1",
            "DiningTable_Idle2",
            "DiningTable_Idle3",
            "DiningTable_Idle4",
            "DiningTable_Idle5",
        };
        private GameObject _characterGo;
        private Animator _characterAnimator;
        private Transform _characterRoot;
        private Transform _characterAnchor;
        private Transform _weaponAnchor;
        private Transform _weaponPivot;
        private Transform _weaponDisplayRoot;
        private Transform _weaponStagingRoot;
        private Transform _weaponInstance;
        private Transform _handWeaponRoot;
        private Transform _handWeaponInstance;
        private int _loadVersion;
        private int _weaponLoadVersion;
        private int _handWeaponLoadVersion;
        private bool _showWeapon;
        private bool _weaponReady;
        private int _currentWeaponBagIndex;
        private int _currentWeaponItemId;
        private int _currentHandWeaponItemId;
        private Camera _detailMainCamera;
        private Camera _detailWeaponCamera;
        private readonly OutGameCharacterDynamicCameraController _detailCameraController = new OutGameCharacterDynamicCameraController();
        private readonly OutGameCharacterDetailActionController _detailActionController = new OutGameCharacterDetailActionController();
        private readonly OutGameCharacterDetailEntranceEffectController _detailEntranceEffectController =
            new OutGameCharacterDetailEntranceEffectController();
        private readonly OutGameWeaponTouchController _weaponTouchController = new OutGameWeaponTouchController();
        private readonly OutGameCharacterDetailWeaponDissolveController _handWeaponDissolveController =
            new OutGameCharacterDetailWeaponDissolveController();
        private readonly OutGameCharacterDetailBasicCameraInteractionController _basicCameraInteractionController =
            new OutGameCharacterDetailBasicCameraInteractionController();
        private readonly OutGameCharacterPresentationService _presentationService =
            new OutGameCharacterPresentationService();
        private readonly RoomCharacterAnimationService _handWeaponAnimationService =
            new RoomCharacterAnimationService();
        private StagePresentationBindingBridge _stageBindingBridge;
        private StageCameraPresentationProvider _stageCameraPresentationProvider;
        private StageCharacterActionPresentationProvider _stageCharacterActionPresentationProvider;
        private StageWeaponPresentationProvider _stageWeaponPresentationProvider;
        private StageControlHandle _cdcWeaponTouchHandle;
        private StageControlHandle _cdcWeaponDissolveHandle;
        private StageControlHandle _cdcWeaponVisibilityHandle;
        private readonly CdcWeaponTouchOwner _cdcWeaponTouchOwner;
        private readonly CdcWeaponDissolveOwner _cdcWeaponDissolveOwner;
        private readonly CdcWeaponVisibilityOwner _cdcWeaponVisibilityOwner;
        private StageControlHandle _cdcCharacterActionHandle;
        private readonly CdcCharacterActionOwner _cdcCharacterActionOwner;
        private readonly DetailCharacterVisualPrewarmScope _detailCharacterVisualPrewarmScope =
            new DetailCharacterVisualPrewarmScope();
        private readonly HashSet<string> _presentationSuspensionRequestIds = new HashSet<string>(); // 持有要求隐藏 CharacterMgr 角色展示的配对请求。
        private Animator _presentationSuspendedAnimator;
        private bool _presentationSuspendedAnimatorOriginalKeepState;
        private int _currentDetailTabIndex;
        private int _detailTabTransitionRequestVersion;
        private int _detailPresentationVersion;
        private int _displayedDetailCharacterCfgId;
        private CharacterDetailWeaponDissolveTransitionPlan _pendingHandWeaponDissolvePlan;
        private string _debugHouseholdCharacterPrefabOverride = string.Empty;
        private int _detailWeaponOverrideBagIndex = -1;
        private int _detailHandWeaponOverrideBagIndex = -1;
        // 快速切换角色时只保留最后一次详情页切换请求，避免旧请求晚到后覆盖新模型。
        private int _characterSwitchRequestVersion;

        private bool _initialized;

        public OutGameCharacterSystem(OutGameModule module)
        {
            _module = module;
            _characterPool = new OutGameCharacterPool();
            _cdcCharacterActionOwner = new CdcCharacterActionOwner(this);
            _cdcWeaponTouchOwner = new CdcWeaponTouchOwner(this);
            _cdcWeaponDissolveOwner = new CdcWeaponDissolveOwner(this);
            _cdcWeaponVisibilityOwner = new CdcWeaponVisibilityOwner(this);
        }

        internal OutGameCharacterPool CharacterPool => _characterPool;

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _detailActionController.BindEntranceEffectController(_detailEntranceEffectController);
            _detailActionController.OnTransitionInitialActionStarted = OnDetailTransitionInitialActionStarted;
            _detailActionController.OnActionPlayed = OnDetailActionPlayed;
            _handWeaponDissolveController.EnsureConfigLoadedAsync().Forget();

            // 监听角色切换事件
            GameEvent.AddEventListener<int>(UIEventDefine.OnCharacterSwitch, OnCharacterSwitch);

            // 角色表现刷新回包后再尝试创建/刷新模型（常见：UI 先打开再拉取数据）
            GameEvent.AddEventListener(UIEventDefine.OnCharacterPresentationRefresh, OnCharacterPresentationRefresh);

            GameEvent.AddEventListener<bool>(UIEventDefine.OnCharacterSwitchShowWeapon, OnCharacterSwitchShowWeapon);
            GameEvent.AddEventListener<int>(UIEventDefine.OnCharacterDetailMainTabChanged, OnCharacterDetailMainTabChanged);
            GameEvent.AddEventListener<bool>(UIEventDefine.OnOutGameHandWeaponConstraintModeChanged, OnHandWeaponConstraintModeChanged);
            GameEvent.AddEventListener(UIEventDefine.OnBagListUpdate, OnBagListUpdate);
            GameEvent.AddEventListener(UIEventDefine.OnCharacterEquipUpdate, OnCharacterEquipUpdate);
            GameEvent.AddEventListener<string>(UIEventDefine.OnCharacterEquipUpdate, OnCharacterEquipUpdate);

            _initialized = true;
        }

        public void Shutdown()
        {
            if (!_initialized)
            {
                return;
            }

            GameEvent.RemoveEventListener<int>(UIEventDefine.OnCharacterSwitch, OnCharacterSwitch);
            GameEvent.RemoveEventListener(UIEventDefine.OnCharacterPresentationRefresh, OnCharacterPresentationRefresh);
            GameEvent.RemoveEventListener<bool>(UIEventDefine.OnCharacterSwitchShowWeapon, OnCharacterSwitchShowWeapon);
            GameEvent.RemoveEventListener<int>(UIEventDefine.OnCharacterDetailMainTabChanged, OnCharacterDetailMainTabChanged);
            GameEvent.RemoveEventListener<bool>(UIEventDefine.OnOutGameHandWeaponConstraintModeChanged, OnHandWeaponConstraintModeChanged);
            GameEvent.RemoveEventListener(UIEventDefine.OnBagListUpdate, OnBagListUpdate);
            GameEvent.RemoveEventListener(UIEventDefine.OnCharacterEquipUpdate, OnCharacterEquipUpdate);
            GameEvent.RemoveEventListener<string>(UIEventDefine.OnCharacterEquipUpdate, OnCharacterEquipUpdate);
            _detailActionController.OnTransitionInitialActionStarted = null;
            _detailActionController.OnActionPlayed = null;
            _initialized = false;

            Reset();
            _characterPool.Shutdown();
            _currentDetailTabIndex = 0;
        }

        public void Reset()
        {
            // 使所有未完成的异步加载失效，避免 Logout/切场景后“晚到”挂回模型
            _loadVersion++;
            _weaponLoadVersion++;
            _handWeaponLoadVersion++;
            _detailTabTransitionRequestVersion++;
            _detailPresentationVersion++;
            _pendingHandWeaponDissolvePlan = null;

            // 先取消详情镜头/动作链，避免异步任务继续访问即将销毁的旧角色。
            _detailCameraController.CancelTransition();
            _detailActionController.CancelPendingAction();
            ReleaseCdcCharacterActionControl();
            ReleaseCdcWeaponPresentationControl();
            _detailEntranceEffectController.CancelAndClear();
            _handWeaponDissolveController.CancelPending();
            _weaponTouchController.CancelDrag();
            _basicCameraInteractionController.Reset();
            _detailCharacterVisualPrewarmScope.Restore();
            RestorePresentationSuspendedAnimatorSetting("Reset");
            _presentationSuspensionRequestIds.Clear();

            if (_characterRoot != null)
            {
                ResetCharacterRootTransform(_characterRoot);
                HideAllChildren(_characterRoot);
            }
            HideCurrentCharacterInstance();
            _characterAnchor = null;

            HideWeaponDisplayHierarchy();
            ClearCurrentHandWeaponDisplay();
            _weaponAnchor = null;
            _weaponReady = false;
            _currentWeaponBagIndex = 0;
            _currentWeaponItemId = 0;
            _currentHandWeaponItemId = 0;
            _displayedDetailCharacterCfgId = 0;
            _detailWeaponOverrideBagIndex = -1;
            _detailHandWeaponOverrideBagIndex = -1;
            ResetDetailWeaponCameraVisibility();
            _detailCameraController.Reset();
            _detailActionController.Reset();
            _detailEntranceEffectController.CancelAndClear();
            _handWeaponDissolveController.Reset();
            _weaponTouchController.Reset();
            _basicCameraInteractionController.Reset();

            _characterPool.Reset();
        }

        public Transform GetCurrentCharacterTransform()
        {
            return _characterGo != null ? _characterGo.transform : null;
        }

        internal void DetachCurrentCharacterBeforeStageRelease(
            OutGameStageKey stageKey,
            GameObject stageInstance)
        {
            if (stageKey == OutGameStageKey.Household ||
                stageKey == OutGameStageKey.Team ||
                stageInstance == null ||
                _characterGo == null ||
                !_characterGo.transform.IsChildOf(stageInstance.transform))
            {
                return;
            }

            _detailCharacterVisualPrewarmScope.Restore();
            ClearCurrentHandWeaponDisplay();
            UnregisterCharacterMainLightTarget(_characterGo);
            _characterPool.ParkLease(_characterLease);
            _characterRoot = null;
            _characterAnchor = null;
        }

        /// <summary>
        /// 按请求 ID 更新局外角色展示暂停集合，并在首个申请/最后释放时切换 OutGameCharacterRoot。
        /// 新角色若在暂停期间异步加载，后续展示入口也会读取同一集合，避免晚到模型重新出现。
        /// </summary>
        /// <param name="requestId">申请方生成并负责配对释放的唯一 ID。</param>
        /// <param name="suspended">true 添加暂停请求，false 移除同一请求。</param>
        public void SetCurrentCharacterPresentationSuspended(string requestId, bool suspended)
        {
            if (string.IsNullOrWhiteSpace(requestId))
            {
                Log.Warning("[OutGameCharacterSystem] Ignore character presentation suspension with empty requestId.");
                return;
            }

            bool changed = suspended
                ? _presentationSuspensionRequestIds.Add(requestId)
                : _presentationSuspensionRequestIds.Remove(requestId);
            if (!changed)
            {
                return;
            }

            // 把完整 owner 快照同步给当前实例，避免缓存角色切换后只增不减造成暂停租约泄漏。
            RoomCharacterController roomCharacterController = GetCurrentRoomCharacterController();
            roomCharacterController?.SynchronizeHouseholdAutoInteractionSuspensions(
                _presentationSuspensionRequestIds);

            // 只在有效请求集合变化后重算 root，可安全处理重复申请、重复释放和多个 owner 嵌套。
            ApplyCharacterPresentationSuspension();
            Log.Info(
                $"[OutGameCharacterSystem] Character presentation suspension changed. " +
                $"suspended={IsCharacterPresentationSuspended}, requestCount={_presentationSuspensionRequestIds.Count}, " +
                $"requestId={requestId}, character={(_characterGo != null ? _characterGo.name : "null")}");
        }

        public void SetDebugHouseholdCharacterPrefabOverride(string prefabPath)
        {
            _debugHouseholdCharacterPrefabOverride = NormalizePrefabPath(prefabPath);
        }

        public void ClearDebugHouseholdCharacterPrefabOverride()
        {
            _debugHouseholdCharacterPrefabOverride = string.Empty;
        }

        public bool TryExecuteCurrentCharacterInteractionCommand(InterActionIncCommand command,
            RoomInteractionCommandSubmitStrategy submitStrategy = RoomInteractionCommandSubmitStrategy.Append)
        {
            if (command == null)
            {
                return false;
            }

            // 这里只把命令路由到当前卧室角色身上的 RoomCharacterController
            RoomCharacterController roomCharacterController = GetCurrentRoomCharacterController();
            if (roomCharacterController == null)
            {
                return false;
            }

            return roomCharacterController.ExecuteInteractionCommand(command, submitStrategy);
        }

        public bool TryExecuteCurrentCharacterInteractionCommandBatch(IReadOnlyList<InterActionIncCommand> commands,
            RoomInteractionCommandSubmitStrategy submitStrategy = RoomInteractionCommandSubmitStrategy.Append)
        {
            if (commands == null || commands.Count == 0)
            {
                Log.Error(
                    $"[OutGameCharacterSystem] Interaction command batch submit failed. Reason=InputCommandsEmpty, SubmitStrategy={submitStrategy}");
                return false;
            }

            RoomCharacterController roomCharacterController = GetCurrentRoomCharacterController();
            if (roomCharacterController == null)
            {
                Log.Error(
                    $"[OutGameCharacterSystem] Interaction command batch submit failed. Reason=RoomCharacterControllerNull, " +
                    $"CharacterGo={(_characterGo != null ? _characterGo.name : "null")}, CommandCount={commands.Count}, SubmitStrategy={submitStrategy}");
                return false;
            }

            return roomCharacterController.ExecuteInteractionCommandBatch(commands, submitStrategy);
        }

        public bool TryCallCurrentRoomCharacterToPlayer()
        {
            // 模块系统只定位当前角色实例，呼唤的中断策略和餐桌特殊逻辑留给控制器处理。
            RoomCharacterController roomCharacterController = GetCurrentRoomCharacterController();
            if (roomCharacterController == null)
            {
                Log.Warning(
                    $"[OutGameCharacterSystem] Call skipped: RoomCharacterController not found on current character. CharacterGo={(_characterGo != null ? _characterGo.name : "null")}");
                return false;
            }

            return roomCharacterController.TryCallToPlayer();
        }

        public bool CanCallCurrentRoomCharacterToPlayer()
        {
            // 距离阈值配置在 RoomCharacterController.Feature 上，模块层只负责转发查询结果。
            RoomCharacterController roomCharacterController = GetCurrentRoomCharacterController();
            return roomCharacterController != null && roomCharacterController.CanCallToPlayer();
        }

        public bool TryGetCurrentRoomCharacterCallAvailabilityDiagnostic(out bool canCall, out string reason, out float distance)
        {
            RoomCharacterController roomCharacterController = GetCurrentRoomCharacterController();
            if (roomCharacterController != null)
            {
                return roomCharacterController.TryGetCallAvailabilityDiagnostic(out canCall, out reason, out distance);
            }

            canCall = false;
            reason = _characterGo == null ? "CurrentCharacterMissing" : "RoomCharacterControllerMissing";
            distance = 0f;
            return false;
        }

        public bool TryPokeCurrentRoomCharacter(RoomCharacterPlayerSide side)
        {
            // UI 或 helper 已经给出前后语义，这里不再依赖具体场景节点。
            RoomCharacterController roomCharacterController = GetCurrentRoomCharacterController();
            return roomCharacterController != null && roomCharacterController.TryPoke(side);
        }

        public bool TryExitCurrentRoomCharacterInteractionObject()
        {
            RoomCharacterController roomCharacterController = GetCurrentRoomCharacterController();
            if (roomCharacterController == null)
            {
                return false;
            }

            InterActionIncCommand outCommand = new InterActionIncCommand
            {
                CommandType = InterActionCommandType.Out,
                ObjName = string.Empty,
                TargetAnimaitonState = HallCharacterAnimationState.None,
                Duration = 0f,
                FaceObjName = string.Empty,
                CommandSource = RoomInteractionCommandSource.UserInteraction,
                DebugCommandCollectionId = "__room_user_exit_object__",
                DebugCommandIndex = 0,
                DebugCommandCount = 1,
            };
            return roomCharacterController.ExecuteInteractionCommand(outCommand, RoomInteractionCommandSubmitStrategy.InterruptCurrent);
        }

        public bool TryGetCurrentRoomCharacterPlayerInteractionAreaState(out bool isInside)
        {
            return TryGetCurrentRoomCharacterPlayerInteractionAreaState(out isInside, out _);
        }

        public bool TryGetCurrentRoomCharacterPlayerInteractionAreaState(out bool isInside, out int stage)
        {
            // 查询入口只用于 UI 刚打开时同步按钮显隐，避免角色首帧事件早于 UI 注册导致初始靠近状态丢失。
            RoomCharacterController roomCharacterController = GetCurrentRoomCharacterController();
            if (roomCharacterController != null)
            {
                return roomCharacterController.TryGetPlayerInteractionAreaState(out isInside, out stage);
            }

            isInside = false;
            stage = HouseholdMoveController.RoomInteractionStageStanding;
            return false;
        }

        public bool HasCurrentCharacterUnfinishedInteractionCommandSequence()
        {
            RoomCharacterController roomCharacterController = GetCurrentRoomCharacterController();
            if (roomCharacterController == null)
            {
                return false;
            }

            return roomCharacterController.HasUnfinishedInteractionCommandSequence;
        }

        public bool TryPlayCurrentCharacterAnimationByStateName(string stateName, float crossFadeSeconds = 0f, float startNormalizedTime = float.NaN)
        {
            RoomCharacterController roomCharacterController = GetCurrentRoomCharacterController();
            if (roomCharacterController == null)
            {
                return false;
            }

            return roomCharacterController.TryPlayCurrentCharacterAnimationByStateName(stateName, crossFadeSeconds, startNormalizedTime);
        }

        public bool TryInterruptAndPlayCurrentCharacterAnimationByStateName(string stateName, float crossFadeSeconds = 0f, float startNormalizedTime = float.NaN)
        {
            RoomCharacterController roomCharacterController = GetCurrentRoomCharacterController();
            if (roomCharacterController == null)
            {
                return false;
            }

            return roomCharacterController.TryInterruptAndPlayCurrentCharacterAnimationByStateName(stateName, crossFadeSeconds, startNormalizedTime);
        }

        public bool TryRememberCurrentCharacterGenerateIdleActionSnapshot(IReadOnlyList<RoomInteractionActionData> commands, string fallbackInteractionObject)
        {
            RoomCharacterController roomCharacterController = GetCurrentRoomCharacterController();
            if (roomCharacterController == null)
            {
                return false;
            }

            return roomCharacterController.TryRememberGenerateIdleActionSnapshotData(commands, fallbackInteractionObject);
        }

        public bool TryResetCurrentCharacterHouseholdAutoInteractionTimer(bool pauseTimer = false)
        {
            RoomCharacterController roomCharacterController = GetCurrentRoomCharacterController();
            if (roomCharacterController == null)
            {
                return false;
            }

            roomCharacterController.ResetHouseholdAutoInteractionTimer(pauseTimer);
            return true;
        }

        public async UniTask<bool> WaitCurrentCharacterHouseholdAutoInteractionCommandsIdleAsync(CancellationToken cancellationToken)
        {
            RoomCharacterController roomCharacterController = GetCurrentRoomCharacterController();
            if (roomCharacterController == null)
            {
                return false;
            }

            await roomCharacterController.WaitForHouseholdAutoInteractionCommandsIdleAsync(cancellationToken);
            return true;
        }

        private RoomCharacterController GetCurrentRoomCharacterController()
        {
            return ResolveRoomCharacterController(_characterGo);
        }

        private HallCharacterController GetCurrentHallCharacterController()
        {
            return ResolveHallCharacterController(_characterGo);
        }

        private static RoomCharacterController ResolveRoomCharacterController(GameObject characterGo)
        {
            if (characterGo == null)
            {
                return null;
            }

            RoomCharacterController controller = characterGo.GetComponent<RoomCharacterController>();
            return controller != null
                ? controller
                : characterGo.GetComponentInChildren<RoomCharacterController>(true);
        }

        private static HallCharacterController ResolveHallCharacterController(GameObject characterGo)
        {
            if (characterGo == null)
            {
                return null;
            }

            HallCharacterController controller = characterGo.GetComponent<HallCharacterController>();
            return controller != null
                ? controller
                : characterGo.GetComponentInChildren<HallCharacterController>(true);
        }

        public async UniTask ShowLobbyCharacterAsync()
        {
            await EnsureCharacterForContextAsync(OutGameCharacterContext.Lobby);
        }

        internal void DumpDiagnostics(string reason = "Manual")
        {
            StringBuilder builder = new StringBuilder(512);
            builder.Append("[OutGameCharacterDiagnostics]");
            builder.Append($" reason={reason ?? "Manual"}");
            builder.Append($" context={ResolveCurrentContext()}");
            builder.Append($" cfgId={GetCurrentCharacterCfgId(ResolveCurrentContext())}");
            builder.Append($" instanceId={(_characterGo != null ? _characterGo.GetInstanceID() : 0)}");
            builder.Append($" active={(_characterGo != null && _characterGo.activeInHierarchy)}");
            builder.Append($" parent={(_characterGo != null && _characterGo.transform.parent != null ? _characterGo.transform.parent.name : "<null>")}");
            builder.Append($" leaseInUse={(_characterLease != null && _characterLease.InUse)}");
            builder.Append($" leaseReleased={(_characterLease == null || _characterLease.IsReleased)}");
            builder.Append($" stage={_module?.CurrentStageKey}");
            builder.Append($" suspended={IsCharacterPresentationSuspended}");
            builder.Append($" pool={_characterPool.BuildDiagnosticSnapshot()}");
            Log.Info(builder.ToString());
        }

        public async UniTask RefreshForActiveSceneAsync(int requestedCharacterCfgId = 0)
        {
            var context = ResolveCurrentContext();
            if (context == OutGameCharacterContext.Detail)
            {
                // 运行时再次兜底，确保角色详情场景不会继承 CDC 作者态预览残留的相机启用状态。
                CharacterDynamicCameraRuntimeSceneGuard.RestoreSceneCameraState(SceneManager.GetActiveScene());
                _detailCameraController.InvalidateSceneCamera();
            }
            else
            {
                ResetDetailPresentationStateForNonDetailScene();
                _currentDetailTabIndex = 0;
                _detailCameraController.Reset();
                _detailActionController.Reset();
            }

            if (context == OutGameCharacterContext.Unknown)
            {
                // 非局外相关场景，不做处理
                return;
            }

            await EnsureCharacterForContextAsync(context, requestedCharacterCfgId);

            if (context == OutGameCharacterContext.Lobby &&
                !IsCharacterPresentationSuspended &&
                !IsCurrentCharacterActive())
            {
                await UniTask.DelayFrame(1);
                if (ResolveCurrentContext() == OutGameCharacterContext.Lobby)
                {
                    await EnsureCharacterForContextAsync(context, requestedCharacterCfgId);
                }
            }
        }

        private bool IsCurrentCharacterActive()
        {
            return _characterGo != null && _characterGo.activeInHierarchy;
        }

        private void ResetDetailPresentationStateForNonDetailScene()
        {
            _detailCameraController.CancelTransition();
            CancelCdcCharacterAction();
            _detailActionController.CancelPendingAction();
            _detailCharacterVisualPrewarmScope.Restore();
            _characterPool.RestoreVisualState(_characterLease);
            _loadVersion++;
            _weaponLoadVersion++;
            _handWeaponLoadVersion++;
            _characterSwitchRequestVersion++;
            _detailTabTransitionRequestVersion++;
            _detailPresentationVersion++;
            _displayedDetailCharacterCfgId = 0;
            _showWeapon = false;
            _detailWeaponOverrideBagIndex = -1;
            _basicCameraInteractionController.Reset();
            _weaponTouchController.SetActive(false);
            _weaponTouchController.CancelDrag();
            HideDetailHandWeapon(GetCurrentHallCharacterController());

            if (_weaponPivot != null)
            {
                _weaponPivot.gameObject.SetActive(false);
            }

            ResetDetailWeaponCameraVisibility();
        }

        private void OnCharacterSwitch(int cfgId)
        {
            var context = ResolveCurrentContext();
            int requestVersion = ++_characterSwitchRequestVersion;
            if (context == OutGameCharacterContext.Detail)
            {
                _loadVersion++;
                _weaponLoadVersion++;
                _handWeaponLoadVersion++;
                _handWeaponDissolveController.CancelPending();
                _pendingHandWeaponDissolvePlan = null;
                bool keepCommittedHandWeaponVisible =
                    _handWeaponInstance != null && _handWeaponInstance.gameObject.activeInHierarchy;
                if (!keepCommittedHandWeaponVisible)
                {
                    ClearCurrentHandWeaponDisplay();
                }

                // 武器正在显示时，当前角色与武器继续作为已提交画面，直到目标角色和武器准备完成后再原子替换。
                CancelCurrentDetailPresentation(resetBasicCameraPose: false);
                _basicCameraInteractionController.Reset();
            }

            UniTask.Void(async () =>
            {
                await UniTask.DelayFrame(1);
                // 同一帧内可能连续收到多次切换，这里直接丢弃已经过期的旧请求。
                if (requestVersion != _characterSwitchRequestVersion)
                {
                    return;
                }

                await EnsureCharacterForContextAsync(context, cfgId);
                if (requestVersion == _characterSwitchRequestVersion)
                {
                    GameEvent.Send(UIEventDefine.OnCharacterModelLoaded);
                }
            });
        }

        private void OnCharacterPresentationRefresh()
        {
            CharacterDataModel.Instance.PublishAllCharacterCardRedDots();

            // 角色表现数据更新后，尝试在当前局外场景补上模型显示
            UniTask.Void(async () =>
            {
                await UniTask.DelayFrame(1);
                await RefreshForActiveSceneAsync();
            });
        }

        private void OnCharacterSwitchShowWeapon(bool showWeapon)
        {
            _showWeapon = showWeapon;
            Log.Debug($"showWeapon: {_showWeapon}");
            _weaponTouchController.SetActive(false);
            _weaponTouchController.CancelDrag();
            if (_showWeapon)
            {
                _detailEntranceEffectController.CancelAndClear();
                HideDetailHandWeapon(GetCurrentHallCharacterController());
            }

            UpdateCharacterWeaponModeVisibility();
            ApplyWeaponCharacterVisibility();
            if (_showWeapon)
            {
                ScheduleRefreshCurrentDetailWeapon(resetPoseIfSameWeapon: true, reason: "showWeapon");
            }
            else
            {
                ScheduleRefreshCurrentDetailHandWeapon("switchShowWeapon");
                SyncCurrentDetailCharacterAction();
            }
        }

        private void SyncCurrentDetailCharacterAction()
        {
            if (ResolveCurrentContext() != OutGameCharacterContext.Detail || _characterGo == null)
            {
                return;
            }

            int characterCfgId = GetCurrentCharacterCfgId(OutGameCharacterContext.Detail);
            if (characterCfgId <= 0)
            {
                return;
            }

            TryRunCdcCharacterAction(
                new StageCharacterActionRequest
                {
                    ActionId = "SyncCharacterAction",
                    Source = nameof(SyncCurrentDetailCharacterAction)
                },
                () =>
                {
                    _detailActionController.SyncCharacterAction(
                        GetCurrentHallCharacterController(),
                        characterCfgId,
                        Mathf.Max(0, _currentDetailTabIndex));
                    return true;
                });
        }

        private void OnBagListUpdate()
        {
            CharacterDataModel.Instance.PublishAllCharacterCardRedDots();
            ScheduleRefreshCurrentDetailWeapon(resetPoseIfSameWeapon: false, reason: "bagListUpdate");
            ScheduleRefreshCurrentDetailHandWeapon("bagListUpdate");
        }

        private int CancelCurrentDetailPresentation(bool resetBasicCameraPose)
        {
            _detailTabTransitionRequestVersion++;
            int presentationVersion = ++_detailPresentationVersion;
            _detailCameraController.CancelTransition();
            CancelCdcCharacterAction();
            _detailEntranceEffectController.CancelAndClear();
            _basicCameraInteractionController.SetEnabled(false);
            if (resetBasicCameraPose)
            {
                _basicCameraInteractionController.ResetImmediate();
            }

            return presentationVersion;
        }

        private bool IsDetailPresentationRequestValid(
            int presentationVersion,
            int characterCfgId,
            int tabIndex,
            HallCharacterController hallCharacterController)
        {
            if (presentationVersion != _detailPresentationVersion ||
                ResolveCurrentContext() != OutGameCharacterContext.Detail ||
                tabIndex != Mathf.Max(0, _currentDetailTabIndex) ||
                characterCfgId != GetCurrentCharacterCfgId(OutGameCharacterContext.Detail))
            {
                return false;
            }

            return hallCharacterController == null ||
                   ReferenceEquals(hallCharacterController, GetCurrentHallCharacterController());
        }

        private void OnCharacterEquipUpdate()
        {
            CharacterDataModel.Instance.PublishAllCharacterCardRedDots();
            ScheduleRefreshCurrentDetailWeapon(resetPoseIfSameWeapon: false, reason: "characterEquipUpdate");
            ScheduleRefreshCurrentDetailHandWeapon("characterEquipUpdate");
        }

        private void OnCharacterEquipUpdate(string characterId)
        {
            CharacterBrief currentCharacter = CharacterDataModel.Instance.GetDetailCurrentCharacter();
            if (!string.IsNullOrEmpty(characterId) &&
                currentCharacter != null &&
                !string.Equals(currentCharacter.CharacterId, characterId, System.StringComparison.Ordinal))
            {
                CharacterDataModel.Instance.PublishCharacterCardRedDots(characterId);
                CharacterDataModel.Instance.RefreshCharacterListNavRedDot();
                return;
            }

            CharacterDataModel.Instance.PublishCharacterCardRedDots(currentCharacter?.CharacterId ?? characterId);
            CharacterDataModel.Instance.RefreshCharacterListNavRedDot();
            ScheduleRefreshCurrentDetailWeapon(resetPoseIfSameWeapon: false, reason: "characterEquipUpdate");
            ScheduleRefreshCurrentDetailHandWeapon("characterEquipUpdate");
        }

        private void OnHandWeaponConstraintModeChanged(bool enabled)
        {
            _handWeaponLoadVersion++;
            if (_handWeaponInstance == null || _characterGo == null || _currentHandWeaponItemId <= 0)
            {
                return;
            }

            HallCharacterController hallCharacterController = GetCurrentHallCharacterController();
            Transform detachedParent = enabled ? _characterPool.GetHandWeaponManagerRoot() : null;
            if (!_presentationService.TryAttachVisibleHandWeapon(
                    _characterGo,
                    _currentHandWeaponItemId,
                    _handWeaponInstance.gameObject,
                    CharacterLayerName,
                    nameof(OutGameCharacterSystem),
                    enabled,
                    detachedParent,
                    resetDissolve: false,
                    destroyOnFailure: false,
                    out Transform displayRoot))
            {
                Log.Warning(
                    $"[OutGameCharacterSystem] Failed to rebind current hand weapon attachment. " +
                    $"enabled={enabled}, itemId={_currentHandWeaponItemId}, character={_characterGo.name}");
                return;
            }

            _handWeaponRoot = displayRoot;
            if (_characterLease != null)
            {
                _characterLease.HandWeaponGo = _handWeaponInstance.gameObject;
                _characterLease.CurrentWeaponItemId = _currentHandWeaponItemId;
            }

            if (hallCharacterController != null)
            {
                ConfigureHandWeaponDissolve(
                    _handWeaponInstance.gameObject,
                    hallCharacterController,
                    _pendingHandWeaponDissolvePlan,
                    forceVisibleWithoutDissolve: false);
            }

            CharacterMainLightController.NotifyTargetsChanged();
        }

        public void CancelCharacterDynamicCameraTransition()
        {
            CancelCurrentDetailPresentation(resetBasicCameraPose: false);
            _basicCameraInteractionController.ResetImmediate();
        }

        public void RequestCharacterDetailMainTabTransition(int tabIndex)
        {
            OnCharacterDetailMainTabChanged(tabIndex);
        }

        public void SetCharacterDetailBasicCameraInteractionEnabled(bool enabled)
        {
            if (enabled)
            {
                int tabIndex = Mathf.Max(0, _currentDetailTabIndex);
                if (ResolveCurrentContext() != OutGameCharacterContext.Detail || tabIndex != 0)
                {
                    _basicCameraInteractionController.SetEnabled(false);
                    return;
                }

                BindBasicCameraInteractionState(tabIndex);
            }

            _basicCameraInteractionController.SetEnabled(enabled);
        }

        public void ResetCharacterDetailBasicCameraToInitialState()
        {
            CancelCharacterDynamicCameraTransition();
            _basicCameraInteractionController.ResetImmediate();
        }

        public void UpdateCharacterDetailBasicCameraDrag(Vector2 delta)
        {
            _basicCameraInteractionController.ApplyDragDelta(delta);
        }

        public void UpdateCharacterDetailBasicCameraPinch(float previousDistance, float currentDistance)
        {
            _basicCameraInteractionController.ApplyPinchDelta(previousDistance, currentDistance);
        }

        public void CancelCharacterDetailWeaponTouch()
        {
            if (!TryEnsureCdcWeaponTouchControl())
            {
                return;
            }

            if (_stageWeaponPresentationProvider == null)
            {
                _weaponTouchController.CancelDrag();
                return;
            }

            _cdcWeaponTouchOwner.Handler = request => { _weaponTouchController.CancelDrag(); return true; };
            _stageWeaponPresentationProvider.TrySubmitTouch(_cdcWeaponTouchHandle, new StageWeaponTouchRequest { Operation = "Cancel", Source = nameof(CancelCharacterDetailWeaponTouch) }, out _);
            _cdcWeaponTouchOwner.Handler = null;
        }

        public void TickCharacterDetailWeaponTouch(float deltaTime)
        {
            if (!TryEnsureCdcWeaponTouchControl())
            {
                return;
            }

            if (_stageWeaponPresentationProvider == null)
            {
                _weaponTouchController.Tick(deltaTime);
                return;
            }

            _cdcWeaponTouchOwner.Handler = request => { _weaponTouchController.Tick(request.DeltaTime); return true; };
            _stageWeaponPresentationProvider.TrySubmitTouch(_cdcWeaponTouchHandle, new StageWeaponTouchRequest { Operation = "Tick", DeltaTime = deltaTime, Source = nameof(TickCharacterDetailWeaponTouch) }, out _);
            _cdcWeaponTouchOwner.Handler = null;
        }

        public void SetCharacterDetailWeaponOverride(int bagIndex)
        {
            _detailWeaponOverrideBagIndex = bagIndex;
            if (!_showWeapon || ResolveCurrentContext() != OutGameCharacterContext.Detail)
            {
                return;
            }

            RefreshCurrentDetailWeaponAsync(resetPoseIfSameWeapon: true, reason: "weaponOverride").Forget();
        }

        public void ClearCharacterDetailWeaponOverride()
        {
            bool hadOverride = _detailWeaponOverrideBagIndex > 0;
            _detailWeaponOverrideBagIndex = -1;
            if (!hadOverride || !_showWeapon || ResolveCurrentContext() != OutGameCharacterContext.Detail)
            {
                return;
            }

            ScheduleRefreshCurrentDetailWeapon(resetPoseIfSameWeapon: true, reason: "clearWeaponOverride");
        }

        public void SetCharacterDetailHandWeaponOverride(int bagIndex)
        {
            _detailHandWeaponOverrideBagIndex = bagIndex;
            ScheduleRefreshCurrentDetailHandWeapon("handWeaponOverride");
        }

        public void ClearCharacterDetailHandWeaponOverride()
        {
            bool hadOverride = _detailHandWeaponOverrideBagIndex > 0;
            _detailHandWeaponOverrideBagIndex = -1;
            if (!hadOverride)
            {
                return;
            }

            ScheduleRefreshCurrentDetailHandWeapon("clearHandWeaponOverride");
        }

        public bool ApplyCharacterDynamicConfigJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Log.Warning("[OutGameCharacterSystem] ApplyCharacterDynamicConfigJson failed because json is empty.");
                return false;
            }

            if (!CharacterDynamicCameraConfig.TryParse(json, out CharacterDynamicCameraConfig config) || config == null)
            {
                Log.Warning("[OutGameCharacterSystem] ApplyCharacterDynamicConfigJson failed because json is invalid.");
                return false;
            }

            _detailCameraController.ApplyConfig(config);
            _detailActionController.ApplyConfig(config);

            var context = ResolveCurrentContext();
            if (context != OutGameCharacterContext.Detail)
            {
                Log.Info($"[OutGameCharacterSystem] Applied character detail camera config in non-detail context={context}. It will take effect when detail context becomes active.");
                return true;
            }

            int characterCfgId = GetCurrentCharacterCfgId(context);
            if (characterCfgId <= 0)
            {
                Log.Warning("[OutGameCharacterSystem] Applied character detail camera config but current detail character is invalid.");
                return true;
            }

            SyncDetailCharacterState(characterCfgId);
            Log.Info($"[OutGameCharacterSystem] Applied character detail camera config to current detail character. characterCfgId={characterCfgId}.");
            return true;
        }

        public bool ApplyCharacterDetailWeaponTouchConfigJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Log.Warning("[OutGameCharacterSystem] ApplyCharacterDetailWeaponTouchConfigJson failed because json is empty.");
                return false;
            }

            if (!CharacterDetailWeaponTouchConfig.TryParse(json, out CharacterDetailWeaponTouchConfig config) || config == null)
            {
                Log.Warning("[OutGameCharacterSystem] ApplyCharacterDetailWeaponTouchConfigJson failed because json is invalid.");
                return false;
            }

            _weaponTouchController.ApplyConfig(config);
            Log.Info($"[OutGameCharacterSystem] Applied weapon touch config. currentWeaponBagIndex={_currentWeaponBagIndex}, scene={SceneManager.GetActiveScene().name}.");
            return true;
        }

        public bool ApplyCharacterDetailWeaponDissolveConfigJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Log.Warning("[OutGameCharacterSystem] ApplyCharacterDetailWeaponDissolveConfigJson failed because json is empty.");
                return false;
            }

            if (!CharacterDetailWeaponDissolveConfig.TryParse(json, out CharacterDetailWeaponDissolveConfig config) || config == null)
            {
                Log.Warning("[OutGameCharacterSystem] ApplyCharacterDetailWeaponDissolveConfigJson failed because json is invalid.");
                return false;
            }

            _pendingHandWeaponDissolvePlan = null;
            _handWeaponDissolveController.ApplyConfig(config);
            _handWeaponLoadVersion++;

            if (ResolveCurrentContext() == OutGameCharacterContext.Detail)
            {
                HallCharacterController hallCharacterController = GetCurrentHallCharacterController();
                int characterCfgId = GetCurrentCharacterCfgId(OutGameCharacterContext.Detail);
                if (IsDetailHandWeaponDissolveTab(hallCharacterController, characterCfgId, _currentDetailTabIndex))
                {
                    ScheduleRefreshCurrentDetailHandWeapon("dissolveConfigApplied");
                }
                else
                {
                    HideDetailHandWeapon(hallCharacterController);
                }
            }

            Log.Info($"[OutGameCharacterSystem] Applied hand weapon dissolve config. scene={SceneManager.GetActiveScene().name}.");
            return true;
        }

        public bool BeginCharacterDetailWeaponTouch(int pointerId, Vector2 screenPosition, Vector2 touchAreaSize)
        {
            if (!TryEnsureCdcWeaponTouchControl())
            {
                return false;
            }

            if (_stageWeaponPresentationProvider == null)
            {
                return _weaponTouchController.TryBeginDrag(pointerId, screenPosition, touchAreaSize);
            }

            bool accepted = false;
            _cdcWeaponTouchOwner.Handler = request => accepted = _weaponTouchController.TryBeginDrag(request.PointerId, request.ScreenPosition, request.SurfaceSize);
            _stageWeaponPresentationProvider.TrySubmitTouch(_cdcWeaponTouchHandle, new StageWeaponTouchRequest { Operation = "Begin", PointerId = pointerId, ScreenPosition = screenPosition, SurfaceSize = touchAreaSize }, out _);
            _cdcWeaponTouchOwner.Handler = null;
            return accepted;
        }

        public void UpdateCharacterDetailWeaponTouch(int pointerId, Vector2 screenPosition, Vector2 delta, Vector2 touchAreaSize)
        {
            if (!TryEnsureCdcWeaponTouchControl())
            {
                return;
            }

            if (_stageWeaponPresentationProvider == null)
            {
                _weaponTouchController.UpdateDrag(pointerId, screenPosition, delta, touchAreaSize);
                return;
            }

            _cdcWeaponTouchOwner.Handler = request => { _weaponTouchController.UpdateDrag(request.PointerId, request.ScreenPosition, request.Delta, request.SurfaceSize); return true; };
            _stageWeaponPresentationProvider.TrySubmitTouch(_cdcWeaponTouchHandle, new StageWeaponTouchRequest { Operation = "Update", PointerId = pointerId, ScreenPosition = screenPosition, Delta = delta, SurfaceSize = touchAreaSize }, out _);
            _cdcWeaponTouchOwner.Handler = null;
        }

        public void EndCharacterDetailWeaponTouch(int pointerId)
        {
            if (!TryEnsureCdcWeaponTouchControl())
            {
                return;
            }

            if (_stageWeaponPresentationProvider == null)
            {
                _weaponTouchController.EndDrag(pointerId);
                return;
            }

            _cdcWeaponTouchOwner.Handler = request => { _weaponTouchController.EndDrag(request.PointerId); return true; };
            _stageWeaponPresentationProvider.TrySubmitTouch(_cdcWeaponTouchHandle, new StageWeaponTouchRequest { Operation = "End", PointerId = pointerId }, out _);
            _cdcWeaponTouchOwner.Handler = null;
        }

        private void ScheduleRefreshCurrentDetailWeapon(bool resetPoseIfSameWeapon, string reason)
        {
            if (ResolveCurrentContext() != OutGameCharacterContext.Detail)
            {
                return;
            }

            UniTask.Void(async () =>
            {
                await UniTask.DelayFrame(1);
                await RefreshCurrentDetailWeaponAsync(resetPoseIfSameWeapon, reason);
            });
        }

        private async UniTask RefreshCurrentDetailWeaponAsync(bool resetPoseIfSameWeapon, string reason)
        {
            if (ResolveCurrentContext() != OutGameCharacterContext.Detail)
            {
                return;
            }

            bool forceReload = IsWeaponRefreshForceReloadReason(reason);

            int viewModeWeaponItemId = CharacterDataModel.Instance.ViewModeWeaponItemId;
            if (viewModeWeaponItemId > 0)
            {
                await SetWeaponDetailByItemId(viewModeWeaponItemId, -1, resetPoseIfSameWeapon, forceReload);
                return;
            }

            if (_detailWeaponOverrideBagIndex > 0)
            {
                await SetWeaponDetail(_detailWeaponOverrideBagIndex, resetPoseIfSameWeapon, forceReload);
                return;
            }

            int characterCfgId = GetCurrentCharacterCfgId(OutGameCharacterContext.Detail);
            if (characterCfgId <= 0)
            {
                return;
            }

            CharacterDetail characterDetail = CharacterDataModel.Instance.GetCharacterDetailByCfgId(characterCfgId);
            if (characterDetail == null)
            {
                Log.Debug($"[OutGameCharacterSystem] Skip refresh detail weapon because character detail is missing. characterCfgId={characterCfgId}, reason={reason}.");
                return;
            }

            await SetWeaponDetail(characterDetail.WeaponItemIndex, resetPoseIfSameWeapon, forceReload);
        }

        private void OnCharacterDetailMainTabChanged(int tabIndex)
        {
            _handWeaponDissolveController.EnsureConfigLoadedAsync().Forget();
            int previousTabIndex = Mathf.Max(0, _currentDetailTabIndex);
            if (tabIndex >= 0)
            {
                _currentDetailTabIndex = tabIndex;
            }

            var context = ResolveCurrentContext();
            if (context != OutGameCharacterContext.Detail)
            {
                Log.Debug($"[OutGameCharacterSystem] Ignore detail tab change because context={context}, tabIndex={tabIndex}.");
                CancelCurrentDetailPresentation(resetBasicCameraPose: false);
                HideDetailHandWeapon(GetCurrentHallCharacterController());
                _pendingHandWeaponDissolvePlan = null;
                return;
            }

            int characterCfgId = GetCurrentCharacterCfgId(context);
            HallCharacterController hallCharacterController = GetCurrentHallCharacterController();
            CharacterDetailWeaponDissolveTransitionPlan dissolvePlan =
                _handWeaponDissolveController.CreateTransitionPlan(
                    characterCfgId,
                    previousTabIndex,
                    tabIndex,
                    hallCharacterController);
            _pendingHandWeaponDissolvePlan = dissolvePlan;
            Log.Debug(
                $"[OutGameCharacterSystem] Detail tab changed. scene={SceneManager.GetActiveScene().name}, " +
                $"characterCfgId={characterCfgId}, tabIndex={tabIndex}, hallController={(hallCharacterController != null ? hallCharacterController.name : "null")}.");
            SyncDetailHandWeaponForTab(hallCharacterController, characterCfgId, tabIndex, dissolvePlan);
            if (tabIndex == previousTabIndex)
            {
                Log.Debug($"[OutGameCharacterSystem] Ignore duplicate detail tab change. tabIndex={tabIndex}.");
                return;
            }

            int presentationVersion = CancelCurrentDetailPresentation(resetBasicCameraPose: false);
            int transitionRequestVersion = _detailTabTransitionRequestVersion;
            if (tabIndex != 0)
            {
                _basicCameraInteractionController.ClearInteractionStateWithoutApplyingPose();
                _basicCameraInteractionController.ResetCharacterRotationForNonBasicTab();
            }

            PlayDetailTabTransitionAsync(
                hallCharacterController,
                characterCfgId,
                tabIndex,
                transitionRequestVersion,
                presentationVersion).Forget();
        }

        private async UniTaskVoid PlayDetailTabTransitionAsync(
            HallCharacterController hallCharacterController,
            int characterCfgId,
            int tabIndex,
            int transitionRequestVersion,
            int presentationVersion)
        {
            try
            {
                _basicCameraInteractionController.SetCurrentTabIndex(tabIndex);
                if (!IsDetailPresentationRequestValid(presentationVersion, characterCfgId, tabIndex, hallCharacterController))
                {
                    _detailEntranceEffectController.CancelAndClear();
                    return;
                }

                _detailEntranceEffectController.CancelAndClear();
                UniTask<bool> actionStartTask = UniTask.FromResult(false);
                TryRunCdcCharacterAction(
                    new StageCharacterActionRequest
                    {
                        ActionId = "PlayTabAction",
                        Source = nameof(PlayDetailTabTransitionAsync),
                        Interrupt = true
                    },
                    () =>
                    {
                        actionStartTask = _detailActionController.PlayTabActionAndWaitForInitialStartAsync(
                            hallCharacterController,
                            characterCfgId,
                            tabIndex);
                        return true;
                    });
                await actionStartTask;
                if (transitionRequestVersion != _detailTabTransitionRequestVersion ||
                    !IsDetailPresentationRequestValid(presentationVersion, characterCfgId, tabIndex, hallCharacterController))
                {
                    return;
                }

                BindDetailDynamicCamera();
                await _detailCameraController.PlayTabTransitionAsync(characterCfgId, tabIndex);
                if (transitionRequestVersion != _detailTabTransitionRequestVersion ||
                    !IsDetailPresentationRequestValid(presentationVersion, characterCfgId, tabIndex, hallCharacterController))
                {
                    return;
                }

                BindBasicCameraInteractionState(tabIndex);
                _basicCameraInteractionController.SetEnabled(tabIndex == 0);
            }
            catch (System.Exception e)
            {
                Log.Error($"[OutGameCharacterSystem] Detail tab transition failed: {e.Message}\n{e.StackTrace}");
                if (transitionRequestVersion == _detailTabTransitionRequestVersion &&
                    IsDetailPresentationRequestValid(presentationVersion, characterCfgId, tabIndex, hallCharacterController))
                {
                    _basicCameraInteractionController.SetCurrentTabIndex(tabIndex);
                    _basicCameraInteractionController.SetEnabled(tabIndex == 0);
                }
            }
        }

        private void OnDetailTransitionInitialActionStarted(CharacterDetailTransitionActionStartedContext context)
        {
            if (context == null)
            {
                return;
            }

            CharacterDetailWeaponDissolveTransitionPlan plan = _pendingHandWeaponDissolvePlan;
            if (!IsHandWeaponDissolvePlanStillValid(plan, context.CharacterId, context.TabIndex))
            {
                Log.Debug(
                    $"[OutGameCharacterSystem] Ignore hand weapon dissolve hook because plan is stale. " +
                    $"characterId={context.CharacterId}, tabIndex={context.TabIndex}, action={context.ActionState}, " +
                    $"actionSource={context.ActionSource}, requestVersion={context.RequestVersion}.");
                return;
            }

            plan.CdcActionStarted = true;
            plan.ActionContext = context;
            if (plan.IsEnter)
            {
                TryScheduleHandWeaponDissolveIn(plan);
                return;
            }

            if (plan.IsExit)
            {
                TryScheduleHandWeaponDissolveOut(plan);
            }
        }

        private void OnDetailActionPlayed(CharacterDetailActionPlayedContext context)
        {
            if (context == null ||
                ResolveCurrentContext() != OutGameCharacterContext.Detail ||
                _handWeaponInstance == null ||
                context.CharacterId != GetCurrentCharacterCfgId(OutGameCharacterContext.Detail) ||
                context.TabIndex != Mathf.Max(0, _currentDetailTabIndex))
            {
                return;
            }

            Animator[] animators = _handWeaponInstance.GetComponentsInChildren<Animator>(true);
            for (int index = 0; index < animators.Length; index++)
            {
                Animator animator = animators[index];
                if (animator == null ||
                    animator.runtimeAnimatorController == null ||
                    !RoomCharacterAnimationService.TryResolveAnimatorPlaybackTarget(
                        animator,
                        context.ActionState,
                        out _,
                        out _))
                {
                    continue;
                }

                bool played = _handWeaponAnimationService.TryPlayAnimationImmediateByStateName(
                    animator,
                    null,
                    context.ActionState,
                    null,
                    $"CharacterDetail:{context.ActionSource}:weapon",
                    true,
                    (_, failureReason) => Log.Warning(
                        $"[OutGameCharacterSystem] Hand weapon animation play failed. " +
                        $"weapon={_handWeaponInstance.name}, action={context.ActionState}, reason={failureReason}"),
                    context.CrossFadeSeconds,
                    context.StartNormalizedTime);
                if (played)
                {
                    return;
                }
            }
        }

        private void SyncCurrentDetailHandWeaponAnimation()
        {
            if (ResolveCurrentContext() != OutGameCharacterContext.Detail ||
                _handWeaponInstance == null)
            {
                return;
            }

            int characterCfgId = GetCurrentCharacterCfgId(OutGameCharacterContext.Detail);
            int tabIndex = Mathf.Max(0, _currentDetailTabIndex);
            if (_detailActionController.TryGetLastActionPlayedContext(
                    characterCfgId,
                    tabIndex,
                    out CharacterDetailActionPlayedContext context))
            {
                OnDetailActionPlayed(context);
            }
        }

        private async UniTask ApplyDefaultDetailEntryStateAsync(
            int characterCfgId = 0,
            bool preferImmediateAction = false,
            bool allowDetailEnter = false,
            int presentationVersion = 0)
        {
            if (presentationVersion <= 0)
            {
                presentationVersion = _detailPresentationVersion;
            }

            if (characterCfgId <= 0)
            {
                characterCfgId = GetCurrentCharacterCfgId(OutGameCharacterContext.Detail);
            }

            if (characterCfgId <= 0)
            {
                return;
            }

            int tabIndex = Mathf.Max(0, _currentDetailTabIndex);
            HallCharacterController hallCharacterController = GetCurrentHallCharacterController();
            if (!IsDetailPresentationRequestValid(presentationVersion, characterCfgId, tabIndex, hallCharacterController))
            {
                return;
            }

            var hallController = hallCharacterController != null ? hallCharacterController.name : "null";
            Log.Debug(
                $"[OutGameCharacterSystem] Apply default detail entry state. characterCfgId={characterCfgId}, tabIndex={tabIndex}, " +
                $"hallController={hallController}.");
            BindDetailDynamicCamera();
            await _detailCameraController.SyncCharacterPoseWithIdleAsync(characterCfgId, tabIndex);
            if (!IsDetailPresentationRequestValid(presentationVersion, characterCfgId, tabIndex, hallCharacterController))
            {
                return;
            }

            BindBasicCameraInteractionState(tabIndex);
            _basicCameraInteractionController.SetEnabled(tabIndex == 0);
            if (!TryEnsureCdcCharacterActionControl())
            {
                return;
            }

            if (preferImmediateAction &&
                allowDetailEnter &&
                tabIndex == 0 &&
                await _detailActionController.TryPlayDetailEnterActionImmediateAsync(
                    hallCharacterController,
                    characterCfgId,
                    tabIndex))
            {
                if (!IsDetailPresentationRequestValid(presentationVersion, characterCfgId, tabIndex, hallCharacterController))
                {
                    _detailEntranceEffectController.CancelAndClear();
                    return;
                }

                return;
            }

            if (preferImmediateAction)
            {
                _detailEntranceEffectController.CancelAndClear();
                if (!IsDetailPresentationRequestValid(presentationVersion, characterCfgId, tabIndex, hallCharacterController))
                {
                    return;
                }

                if (_detailActionController.TryPlayCharacterSpawnActionImmediate(
                        hallCharacterController,
                        characterCfgId,
                        tabIndex))
                {
                    return;
                }
            }

            _detailEntranceEffectController.CancelAndClear();
            if (!IsDetailPresentationRequestValid(presentationVersion, characterCfgId, tabIndex, hallCharacterController))
            {
                return;
            }

            TryRunCdcCharacterAction(
                new StageCharacterActionRequest
                {
                    ActionId = "SyncCharacterAction",
                    Source = nameof(ApplyDefaultDetailEntryStateAsync)
                },
                () =>
                {
                    _detailActionController.SyncCharacterAction(hallCharacterController, characterCfgId, tabIndex);
                    return true;
                });
        }

        private void SyncDetailCharacterState(int characterCfgId)
        {
            SyncDetailCharacterStateAsync(characterCfgId, _detailPresentationVersion).Forget();
        }

        private async UniTaskVoid SyncDetailCharacterStateAsync(int characterCfgId, int presentationVersion)
        {
            if (characterCfgId <= 0)
            {
                return;
            }

            try
            {
                int tabIndex = Mathf.Max(0, _currentDetailTabIndex);
                HallCharacterController hallCharacterController = GetCurrentHallCharacterController();
                if (!IsDetailPresentationRequestValid(presentationVersion, characterCfgId, tabIndex, hallCharacterController))
                {
                    return;
                }

                BindDetailDynamicCamera();
                await _detailCameraController.SyncCharacterPoseWithIdleAsync(characterCfgId, tabIndex);
                if (!IsDetailPresentationRequestValid(presentationVersion, characterCfgId, tabIndex, hallCharacterController))
                {
                    return;
                }

                BindBasicCameraInteractionState(tabIndex);
                _basicCameraInteractionController.SetEnabled(tabIndex == 0);
                if (_detailActionController.IsDetailEnterActionPlaying(hallCharacterController, characterCfgId, tabIndex))
                {
                    return;
                }

                _detailEntranceEffectController.CancelAndClear();
                if (!IsDetailPresentationRequestValid(presentationVersion, characterCfgId, tabIndex, hallCharacterController))
                {
                    return;
                }

                TryRunCdcCharacterAction(
                    new StageCharacterActionRequest
                    {
                        ActionId = "SyncCharacterAction",
                        Source = nameof(SyncDetailCharacterStateAsync)
                    },
                    () =>
                    {
                        _detailActionController.SyncCharacterAction(hallCharacterController, characterCfgId, tabIndex);
                        return true;
                    });
            }
            catch (System.Exception e)
            {
                Log.Error($"[OutGameCharacterSystem] Sync detail character state failed: {e.Message}\n{e.StackTrace}");
            }
        }

        private void BindBasicCameraInteractionState(int tabIndex)
        {
            ResolveDetailWeaponCameras();
            _basicCameraInteractionController.BindCharacter(_characterRoot);
            if (_detailMainCamera != null && _detailCameraController.TryGetCurrentFocusPoint(out Vector3 focusPoint))
            {
                _basicCameraInteractionController.BindCamera(_detailMainCamera, focusPoint);
            }
            else
            {
                _basicCameraInteractionController.BindCamera(_detailMainCamera);
            }

            _basicCameraInteractionController.SetCurrentTabIndex(tabIndex);
        }

        private async UniTask EnsureCharacterForContextAsync(OutGameCharacterContext context, int requestedCharacterCfgId = 0)
        {
            if (_characterGo == null && _characterLease != null)
            {
                ReleaseCharacterLease(_characterLease);
                _characterLease = null;
                _characterAnimator = null;
                _characterRoot = null;
            }

            if (context == OutGameCharacterContext.Detail &&
                CharacterDataModel.Instance.ViewModeWeaponItemId > 0)
            {
                await EnsureDetailPreviewWeaponAsync(CharacterDataModel.Instance.ViewModeWeaponItemId);
                return;
            }

            bool useRequestedCharacter = requestedCharacterCfgId > 0
                                         && (context == OutGameCharacterContext.Detail
                                             || context == OutGameCharacterContext.Chat);
            int cfgId = useRequestedCharacter
                ? requestedCharacterCfgId
                : GetCurrentCharacterCfgId(context);
            if (cfgId <= 0)
            {
                Log.Warning("[OutGameCharacterSystem] 当前角色CfgId无效，跳过创建模型");
                return;
            }

            // 若已有模型且角色未变，尝试仅切换上下文能力（如控制器/组件）
            if (_characterGo != null && _characterGo.TryGetComponent(out OutGameCharacterMarker marker))
            {
                if (marker.CharacterCfgId == cfgId &&
                    (marker.Context == context || CanTransferPreviewContext(marker.Context, context)))
                {
                    Transform targetAnchor = FindCharacterAnchorTransform(context);
                    if (targetAnchor == null)
                    {
                        Log.Warning($"[OutGameCharacterSystem] Preview context anchor missing during transfer. Context={context}, CfgId={cfgId}");
                        return;
                    }

                    EnsureCharacterRootAttached(characterRoot: GetOrCreateCharacterRoot(targetAnchor));

                    if (_characterRoot == null || _characterGo == null || _characterGo.transform.parent != _characterRoot)
                    {
                        return;
                    }

                    marker.Context = context;
                    if (_characterLease != null)
                    {
                        _characterLease.Profile = GetPresentationProfile(context);
                        _characterPool.RestoreVisualState(_characterLease);
                    }
                    ApplyAnimatorController(context, _characterGo, _characterAnimator);
                    ApplyContextComponents(_characterGo, _characterAnimator, context);
                    ConfigureLobbyCharacterClickController(_characterGo, context);
                    ShowCharacterExclusively(_characterRoot, _characterGo);
                    SetDisplayedDetailCharacter(context, cfgId);
                    EnsureHouseholdRuntimeStarted(context, _characterGo);
                    await ApplyDetailPresentationAfterCharacterActivatedAsync(cfgId, _characterGo);
                    ApplyWeaponCharacterVisibility();
                    return;
                }
            }
            
            Character characterCfg = CharacterDataModel.Instance.GetCharacterInfoByCfgId(cfgId);
            if (characterCfg == null || string.IsNullOrEmpty(characterCfg.BornSkin_Ref?.ShowModel))
            {
                Log.Error($"[OutGameCharacterSystem] 角色配置缺失或ShowModel为空，CfgId={cfgId}");
                return;
            }

            _characterAnchor = FindCharacterAnchorTransform(context);
            if (_characterAnchor == null)
            {
                Log.Warning($"[OutGameCharacterSystem] 未找到角色锚点，跳过创建模型，Context={context}");
                return;
            }

            // 兼容旧逻辑：若锚点下遗留局外角色实例，先清掉，避免二次登录叠模型
            PurgeLegacyOutGameCharacters(_characterAnchor);

            Transform characterRoot = GetOrCreateCharacterRoot(_characterAnchor);
            _characterRoot = characterRoot;
            ResetCharacterRootTransform(_characterRoot);

            bool prepareDetailCharacterPair = ShouldPrepareDetailCharacterPair(cfgId, context);
            if (prepareDetailCharacterPair &&
                await TryActivateReusedDetailCharacterPairAsync(characterRoot, cfgId, context))
            {
                return;
            }

            if (context == OutGameCharacterContext.Household &&
                TryReuseCharacterFromRoot(characterRoot, cfgId, context))
            {
                EnsureHouseholdRuntimeStarted(context, _characterGo);
                await ApplyDetailPresentationAfterCharacterActivatedAsync(cfgId, _characterGo);
                ApplyWeaponCharacterVisibility();
                return;
            }

            if (!prepareDetailCharacterPair)
            {
                HideAllChildren(characterRoot);
            }

            // 现在不给旋转
            // EnsureRotationController(_characterAnchor, context);
            string prefabPath = characterCfg.BornSkin_Ref?.ShowModel;
            
            // if (context == OutGameCharacterContext.Chat)
            //     prefabPath = "Assets/AssetRaw/Actor/HallShow/HallPlayer009.prefab";
            if (context == OutGameCharacterContext.Household)
                prefabPath = ResolveHouseholdCharacterPrefabPath(characterCfg);
            
            int loadVersion = ++_loadVersion;
            int handWeaponLoadVersion = 0;
            int preparedHandWeaponItemId = 0;
            Transform presentationStagingRoot = null;
            Transform characterLoadParent = characterRoot;
            UniTask<GameObject> handWeaponLoadTask = UniTask.FromResult<GameObject>(null);
            if (prepareDetailCharacterPair)
            {
                presentationStagingRoot = CreateDetailPresentationStagingRoot(characterRoot, loadVersion);
                characterLoadParent = presentationStagingRoot;
                handWeaponLoadVersion = ++_handWeaponLoadVersion;
                if (TryResolveCurrentDetailHandWeapon(cfgId, out preparedHandWeaponItemId, out string weaponModelPath))
                {
                    handWeaponLoadTask = LoadDetailHandWeaponAnchorAsync(weaponModelPath, presentationStagingRoot);
                }
            }

            bool useSharedCharacterPool = context != OutGameCharacterContext.Household;
            OutGameCharacterPool.Lease pooledLease = null;
            UniTask<(OutGameCharacterPool.Lease lease, bool created)> pooledCharacterLoadTask =
                UniTask.FromResult((lease: (OutGameCharacterPool.Lease)null, created: false));
            UniTask<GameObject> characterLoadTask = UniTask.FromResult<GameObject>(null);
            if (useSharedCharacterPool)
            {
                pooledCharacterLoadTask = _characterPool.AcquireAsync(
                    prefabPath,
                    preparedHandWeaponItemId,
                    characterLoadParent,
                    GetPresentationProfile(context),
                    cfgId);
            }
            else
            {
                characterLoadTask = GameModule.Resource.LoadGameObjectAsync(prefabPath, characterLoadParent);
            }

            GameObject preparedHandWeaponGo = await handWeaponLoadTask;
            GameObject go;
            if (useSharedCharacterPool)
            {
                (pooledLease, _) = await pooledCharacterLoadTask;
                go = pooledLease?.CharacterGo;
            }
            else
            {
                go = await characterLoadTask;
            }
            if (go == null)
            {
                Log.Error($"[OutGameCharacterSystem] 加载角色预制体失败: {prefabPath}");
                ReleaseCharacterLease(pooledLease);
                DestroyDetailPresentationStagingRoot(presentationStagingRoot);
                return;
            }

            // 若在 await 期间发生 Reset/再次请求，则丢弃本次加载结果，避免挂回旧模型
            if (!IsCharacterLoadRequestStillValid(loadVersion, go))
            {
                if (presentationStagingRoot != null)
                {
                    ReleaseCharacterLease(pooledLease);
                    DestroyDetailPresentationStagingRoot(presentationStagingRoot);
                }
                else
                {
                    if (pooledLease != null)
                    {
                        ReleaseCharacterLease(pooledLease);
                    }
                    else
                    {
                        go.SetActive(false);
                    }
                }
                return;
            }

            go.name = $"OutGameCharacter_{cfgId}";
            ApplyLayerToHierarchy(go, CharacterLayerName);
            go.SetActive(false);
            if (presentationStagingRoot != null)
            {
                go.transform.SetParent(characterRoot, false);
            }

            if (context == OutGameCharacterContext.Detail)
            {
                await _detailActionController.PreloadConfigAsync();
                if (!IsCharacterLoadRequestStillValid(loadVersion, go))
                {
                    if (pooledLease != null)
                    {
                        ReleaseCharacterLease(pooledLease);
                    }
                    else
                    {
                        go.SetActive(false);
                    }
                    DestroyPreparedDetailHandWeapon(preparedHandWeaponGo);
                    DestroyDetailPresentationStagingRoot(presentationStagingRoot);
                    return;
                }
            }

            OutGameCharacterPool.Lease previousCharacterLease = _characterLease;
            _characterGo = go;
            _characterLease = pooledLease;
            _characterAnimator = go.GetComponentInChildren<Animator>();

            if (_characterAnimator == null)
            {
                Log.Error("[OutGameCharacterSystem] 角色模型缺少 Animator，初始化中止");
                ReleaseCharacterLease(pooledLease);
                DestroyPreparedDetailHandWeapon(preparedHandWeaponGo);
                DestroyDetailPresentationStagingRoot(presentationStagingRoot);
                return;
            }

            if (context == OutGameCharacterContext.Household)
            {
                ApplyAnimatorController(context, go, _characterAnimator, characterCfg);
            }
            else
            {
                ApplyAnimatorController(context, go, _characterAnimator);
            }

            ApplyContextComponents(go, _characterAnimator, context);

            if (context == OutGameCharacterContext.Household)
            {
                ApplyHouseholdInitialHallInteraction(go);
            }

            if (context == OutGameCharacterContext.Detail)
            {
                _detailCharacterVisualPrewarmScope.CaptureAndHide(go.transform);
            }

            if (prepareDetailCharacterPair)
            {
                CommitPreparedDetailHandWeapon(
                    go,
                    cfgId,
                    preparedHandWeaponItemId,
                    preparedHandWeaponGo,
                    handWeaponLoadVersion);
                DestroyDetailPresentationStagingRoot(presentationStagingRoot);
            }

            if (previousCharacterLease != null && previousCharacterLease != pooledLease)
            {
                ReleaseCharacterLease(previousCharacterLease);
            }

            HideWeaponInHandAsync(go).Forget();
            ShowCharacterExclusively(characterRoot, go);
            SetDisplayedDetailCharacter(context, cfgId);
            ConfigureLobbyCharacterClickController(go, context);

            if (context == OutGameCharacterContext.Household)
            {
                RoomCharacterController roomCharacterController = ResolveRoomCharacterController(go);
                roomCharacterController?.TryPrimeInitialHallInteractionPose();
                EnsureHouseholdRuntimeStarted(context, go);
            }

            await ApplyDetailPresentationAfterCharacterActivatedAsync(cfgId, go, loadVersion);
            
            var characterDetail = CharacterDataModel.Instance.GetCharacterDetailByCfgId(cfgId);
            if (characterDetail != null && context == OutGameCharacterContext.Detail)
            {
                if (_weaponAnchor == null)
                {
                    _weaponAnchor = FindWeaponAnchorTransform(OutGameCharacterContext.Detail);
                }

                ApplyWeaponCharacterVisibility();

                if (_weaponAnchor == null)
                {
                    Log.Debug($"[OutGameCharacterSystem] 未找到武器锚点");
                }
                else
                {
                    await SetWeaponDetail(characterDetail.WeaponItemIndex, resetPoseIfSameWeapon: true);
                    if (!IsCharacterLoadRequestStillValid(loadVersion, go))
                    {
                        return;
                    }
                }
            }

            // 打标，便于幂等判断
            if (!IsCharacterLoadRequestStillValid(loadVersion, go))
            {
                return;
            }

            OutGameCharacterMarker m = go.GetComponent<OutGameCharacterMarker>();
            if (m == null)
            {
                m = go.AddComponent<OutGameCharacterMarker>();
            }

            m.CharacterCfgId = cfgId;
            m.Context = context;
        }

        private bool IsCharacterLoadRequestStillValid(int loadVersion, GameObject characterGo)
        {
            // 角色创建链路里有多段 await；每次恢复后都要确认请求版本和目标对象仍然有效。
            return loadVersion == _loadVersion &&
                   characterGo != null &&
                   _characterRoot != null &&
                   characterGo.transform != null;
        }

        /// <summary>
        /// 启动晚加载或缓存复用的 Household 角色前，对齐当前角色展示与自动互动暂停快照。
        /// </summary>
        private void EnsureHouseholdRuntimeStarted(OutGameCharacterContext context, GameObject characterGo)
        {
            if (context != OutGameCharacterContext.Household || characterGo == null)
            {
                return;
            }

            RoomCharacterController roomCharacterController = ResolveRoomCharacterController(characterGo);
            if (roomCharacterController != null)
            {
                // 角色可能晚加载或从缓存复用，必须同时补齐有效 owner 并移除实例遗留的旧 owner。
                roomCharacterController.SynchronizeHouseholdAutoInteractionSuspensions(
                    _presentationSuspensionRequestIds);
            }

            roomCharacterController?.StartHouseholdAutoInteractionLoop();
        }

        private static bool IsWeaponRefreshForceReloadReason(string reason)
        {
            return reason == "weaponOverride" ||
                   reason == "bagListUpdate" ||
                   reason == "characterEquipUpdate";
        }

        private bool ShouldPrepareDetailCharacterPair(int characterCfgId, OutGameCharacterContext context)
        {
            return context == OutGameCharacterContext.Detail &&
                   !_showWeapon &&
                   _handWeaponDissolveController.IsVisibleTab(characterCfgId, null, _currentDetailTabIndex);
        }

        private static Transform CreateDetailPresentationStagingRoot(Transform characterRoot, int loadVersion)
        {
            if (characterRoot == null)
            {
                return null;
            }

            var stagingRootGo = new GameObject($"{DetailPresentationStagingRootName}_{loadVersion}");
            Transform stagingRoot = stagingRootGo.transform;
            stagingRoot.SetParent(characterRoot, false);
            stagingRoot.localPosition = Vector3.zero;
            stagingRoot.localRotation = Quaternion.identity;
            stagingRoot.localScale = Vector3.one;
            stagingRootGo.SetActive(false);
            return stagingRoot;
        }

        private static void DestroyDetailPresentationStagingRoot(Transform stagingRoot)
        {
            if (stagingRoot != null)
            {
                Object.Destroy(stagingRoot.gameObject);
            }
        }

        private static void DestroyPreparedDetailHandWeapon(GameObject preparedHandWeaponGo)
        {
            if (preparedHandWeaponGo != null)
            {
                Object.Destroy(preparedHandWeaponGo);
            }
        }

        private async UniTask<GameObject> LoadDetailHandWeaponAnchorAsync(string weaponModelPath, Transform parent)
        {
            return await _presentationService.LoadHandWeaponAnchorAsync(
                weaponModelPath,
                parent,
                nameof(OutGameCharacterSystem));
        }

        private static void ResetLocalTransform(Transform transform)
        {
            if (transform == null)
            {
                return;
            }

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        private void CommitPreparedDetailHandWeapon(
            GameObject characterGo,
            int characterCfgId,
            int weaponItemId,
            GameObject preparedHandWeaponGo,
            int handWeaponLoadVersion)
        {
            HallCharacterController hallCharacterController = ResolveHallCharacterController(characterGo);
            bool requestValid = handWeaponLoadVersion == _handWeaponLoadVersion &&
                                characterCfgId == GetCurrentCharacterCfgId(OutGameCharacterContext.Detail) &&
                                IsDetailHandWeaponDissolveTab(
                                    hallCharacterController,
                                    characterCfgId,
                                    _currentDetailTabIndex);
            if (!requestValid)
            {
                DestroyPreparedDetailHandWeapon(preparedHandWeaponGo);
                return;
            }

            if (weaponItemId <= 0 || preparedHandWeaponGo == null || hallCharacterController == null)
            {
                ClearCurrentHandWeaponDisplay();
                return;
            }

            Transform previousHandWeaponRoot = _handWeaponRoot;
            Transform previousHandWeaponInstance = _handWeaponInstance;
            bool useDetachedConstraint = _module != null && _module.UseDetachedHandWeaponConstraint;
            Transform detachedParent = useDetachedConstraint ? _characterPool.GetHandWeaponManagerRoot() : null;
            if (!_presentationService.TryAttachVisibleHandWeapon(
                    characterGo,
                    weaponItemId,
                    preparedHandWeaponGo,
                    CharacterLayerName,
                    nameof(OutGameCharacterSystem),
                    useDetachedConstraint,
                    detachedParent,
                    resetDissolve: false,
                    destroyOnFailure: true,
                    out Transform nextHandWeaponRoot))
            {
                ClearCurrentHandWeaponDisplay();
                return;
            }

            Transform nextHandWeaponInstance = preparedHandWeaponGo.transform;
            ConfigureHandWeaponDissolve(
                preparedHandWeaponGo,
                hallCharacterController,
                null,
                forceVisibleWithoutDissolve: true);
            preparedHandWeaponGo.SetActive(true);
            RegisterCharacterMainLightTarget(preparedHandWeaponGo);

            // 先完整建立下一份显示状态，再释放上一份；同一角色根复用时只删除旧实例，不删除共用挂点。
            _handWeaponRoot = nextHandWeaponRoot;
            _handWeaponInstance = nextHandWeaponInstance;
            _currentHandWeaponItemId = weaponItemId;
            if (_characterLease != null)
            {
                _characterLease.HandWeaponGo = preparedHandWeaponGo;
                _characterLease.CurrentWeaponItemId = weaponItemId;
            }
            ReleasePreviousHandWeaponDisplay(
                previousHandWeaponRoot,
                previousHandWeaponInstance,
                nextHandWeaponRoot);
            SyncCurrentDetailHandWeaponAnimation();
            CharacterMainLightController.NotifyTargetsChanged();
        }

        private async UniTask EnsureDetailPreviewWeaponAsync(int weaponItemId)
        {
            if (weaponItemId <= 0)
            {
                return;
            }

            _showWeapon = true;
            _detailWeaponOverrideBagIndex = -1;
            await SetWeaponDetailByItemId(weaponItemId, -1, resetPoseIfSameWeapon: true);
        }

        private void SyncDetailHandWeaponForTab(
            HallCharacterController hallCharacterController,
            int characterCfgId,
            int tabIndex,
            CharacterDetailWeaponDissolveTransitionPlan dissolvePlan = null)
        {
            if (!IsDetailHandWeaponDissolveTab(hallCharacterController, characterCfgId, tabIndex) || _showWeapon)
            {
                HideDetailHandWeapon(hallCharacterController, dissolvePlan);
                return;
            }

            RefreshCurrentDetailHandWeaponAsync(hallCharacterController, characterCfgId, "tabChanged", dissolvePlan).Forget();
        }

        private void ScheduleRefreshCurrentDetailHandWeapon(string reason)
        {
            if (_showWeapon || ResolveCurrentContext() != OutGameCharacterContext.Detail)
            {
                return;
            }

            int selectedCharacterCfgId = GetCurrentCharacterCfgId(OutGameCharacterContext.Detail);
            if (_displayedDetailCharacterCfgId <= 0 ||
                selectedCharacterCfgId != _displayedDetailCharacterCfgId)
            {
                return;
            }

            HallCharacterController hallCharacterController = GetCurrentHallCharacterController();
            int characterCfgId = _displayedDetailCharacterCfgId;
            if (!IsDetailHandWeaponDissolveTab(hallCharacterController, characterCfgId, _currentDetailTabIndex) ||
                hallCharacterController == null)
            {
                return;
            }

            RefreshCurrentDetailHandWeaponAsync(hallCharacterController, characterCfgId, reason).Forget();
        }

        private async UniTask RefreshCurrentDetailHandWeaponAsync(
            HallCharacterController hallCharacterController,
            int characterCfgId,
            string reason,
            CharacterDetailWeaponDissolveTransitionPlan dissolvePlan = null)
        {
            int handWeaponLoadVersion = ++_handWeaponLoadVersion;
            try
            {
                if (hallCharacterController == null)
                {
                    ClearCurrentHandWeaponDisplay();
                    return;
                }

                if (!IsDetailHandWeaponRequestValid(handWeaponLoadVersion, characterCfgId, hallCharacterController))
                {
                    return;
                }

                hallCharacterController.SetEnableWeaponNodes(true);
                if (!TryResolveCurrentDetailHandWeapon(characterCfgId, out int weaponItemId, out string weaponModelPath))
                {
                    Log.Debug($"[OutGameCharacterSystem] Skip hand weapon because current character has no weapon. characterCfgId={characterCfgId}, reason={reason}.");
                    ClearCurrentHandWeaponDisplay();
                    return;
                }

                if (!hallCharacterController.TryGetFirstWeaponNode(out Transform weaponNode))
                {
                    Log.Warning($"[OutGameCharacterSystem] Missing first weapon node for hand weapon. characterCfgId={characterCfgId}, weaponItemId={weaponItemId}.");
                    ClearCurrentHandWeaponDisplay();
                    return;
                }

                if (!_module.UseDetachedHandWeaponConstraint &&
                    _handWeaponRoot != null &&
                    _handWeaponRoot.parent != weaponNode)
                {
                    ClearCurrentHandWeaponDisplay();
                }

                _handWeaponRoot = _presentationService.EnsureHandWeaponDisplayRoot(weaponNode);
                if (_handWeaponRoot == null)
                {
                    ClearCurrentHandWeaponDisplay();
                    return;
                }

                if (_handWeaponInstance != null && _currentHandWeaponItemId == weaponItemId)
                {
                    bool useDetachedConstraint = _module != null && _module.UseDetachedHandWeaponConstraint;
                    Transform detachedParent = useDetachedConstraint ? _characterPool.GetHandWeaponManagerRoot() : null;
                    if (!_presentationService.TryAttachVisibleHandWeapon(
                            characterGo: _characterGo,
                            weaponItemId: weaponItemId,
                            handWeaponAnchorGo: _handWeaponInstance.gameObject,
                            layerName: CharacterLayerName,
                            logTag: nameof(OutGameCharacterSystem),
                            useDetachedConstraint: useDetachedConstraint,
                            detachedParent: detachedParent,
                            resetDissolve: false,
                            destroyOnFailure: false,
                            out Transform reboundDisplayRoot))
                    {
                        return;
                    }

                    _handWeaponRoot = reboundDisplayRoot;
                    if (_characterLease != null)
                    {
                        _characterLease.HandWeaponGo = _handWeaponInstance.gameObject;
                        _characterLease.CurrentWeaponItemId = weaponItemId;
                    }
                    ResetHandWeaponAnimators(_handWeaponInstance);
                    ConfigureHandWeaponDissolve(
                        _handWeaponInstance.gameObject,
                        hallCharacterController,
                        dissolvePlan,
                        ShouldForceHandWeaponVisibleOnRefresh(reason));
                    _handWeaponRoot.gameObject.SetActive(true);
                    _handWeaponInstance.gameObject.SetActive(true);
                    SyncCurrentDetailHandWeaponAnimation();
                    if (dissolvePlan != null && dissolvePlan.IsEnter && dissolvePlan.CdcActionStarted)
                    {
                        TryScheduleHandWeaponDissolveIn(dissolvePlan);
                    }

                    CharacterMainLightController.NotifyTargetsChanged();
                    return;
                }

                ClearCurrentHandWeaponInstance();
                Log.Debug($"[OutGameCharacterSystem] Load hand weapon {weaponModelPath}, itemId={weaponItemId}, reason={reason}.");
                GameObject weaponGo = await LoadDetailHandWeaponAnchorAsync(weaponModelPath, _handWeaponRoot);
                if (weaponGo == null)
                {
                    Log.Warning($"[OutGameCharacterSystem] Load hand weapon prefab failed: {weaponModelPath}");
                    return;
                }

                if (!IsDetailHandWeaponRequestValid(handWeaponLoadVersion, characterCfgId, hallCharacterController) ||
                    _handWeaponRoot == null)
                {
                    Object.Destroy(weaponGo);
                    return;
                }

                bool shouldUseDetachedConstraint = _module != null && _module.UseDetachedHandWeaponConstraint;
                Transform detachedWeaponParent = shouldUseDetachedConstraint
                    ? _characterPool.GetHandWeaponManagerRoot()
                    : null;
                if (!_presentationService.TryAttachVisibleHandWeapon(
                        _characterGo,
                        weaponItemId,
                        weaponGo,
                        CharacterLayerName,
                        nameof(OutGameCharacterSystem),
                        shouldUseDetachedConstraint,
                        detachedWeaponParent,
                        resetDissolve: false,
                        destroyOnFailure: true,
                        out Transform loadedDisplayRoot))
                {
                    return;
                }

                Transform weaponTransform = weaponGo.transform;
                _handWeaponRoot = loadedDisplayRoot;
                ConfigureHandWeaponDissolve(
                    weaponGo,
                    hallCharacterController,
                    dissolvePlan,
                    ShouldForceHandWeaponVisibleOnRefresh(reason));
                weaponGo.SetActive(true);
                _handWeaponInstance = weaponTransform;
                _currentHandWeaponItemId = weaponItemId;
                if (_characterLease != null)
                {
                    _characterLease.HandWeaponGo = weaponGo;
                    _characterLease.CurrentWeaponItemId = weaponItemId;
                }
                SyncCurrentDetailHandWeaponAnimation();
                RegisterCharacterMainLightTarget(weaponGo);
                if (dissolvePlan != null && dissolvePlan.IsEnter && dissolvePlan.CdcActionStarted)
                {
                    TryScheduleHandWeaponDissolveIn(dissolvePlan);
                }

                CharacterMainLightController.NotifyTargetsChanged();
            }
            catch (System.Exception e)
            {
                Log.Error($"[OutGameCharacterSystem] Refresh hand weapon failed: {e.Message}\n{e.StackTrace}");
            }
        }

        private static void ResetHandWeaponAnimators(Transform handWeaponInstance)
        {
            if (handWeaponInstance == null)
            {
                return;
            }

            Animator[] animators = handWeaponInstance.GetComponentsInChildren<Animator>(true);
            for (int index = 0; index < animators.Length; index++)
            {
                Animator animator = animators[index];
                if (animator == null || animator.runtimeAnimatorController == null)
                {
                    continue;
                }

                animator.Rebind();
                animator.Update(0f);
            }
        }

        private bool TryResolveCurrentDetailHandWeapon(int characterCfgId, out int weaponItemId, out string weaponModelPath)
        {
            weaponItemId = 0;
            weaponModelPath = string.Empty;
            if (_detailHandWeaponOverrideBagIndex > 0 &&
                TryResolveDetailHandWeaponByBagIndex(_detailHandWeaponOverrideBagIndex, out weaponItemId, out weaponModelPath))
            {
                return true;
            }

            CharacterDetail characterDetail = CharacterDataModel.Instance.GetCharacterDetailByCfgId(characterCfgId);
            if (characterDetail == null || characterDetail.WeaponItemIndex <= 0)
            {
                return false;
            }

            return TryResolveDetailHandWeaponByBagIndex(characterDetail.WeaponItemIndex, out weaponItemId, out weaponModelPath);
        }

        private bool TryResolveDetailHandWeaponByBagIndex(int bagIndex, out int weaponItemId, out string weaponModelPath)
        {
            return _presentationService.TryResolveHandWeaponByBagIndex(
                bagIndex,
                out weaponItemId,
                out weaponModelPath);
        }

        private bool CanUseCdcWeaponPresentation()
        {
            if (_stageBindingBridge == null)
            {
                OutGameStageControlRegistry.TryGetActiveBindingBridge(out _stageBindingBridge);
            }

            if (_stageBindingBridge != null &&
                _stageBindingBridge.TryGetProvider(
                    OutGameStageControlChannel.WeaponTouch,
                    out StageWeaponPresentationProvider provider,
                    out _))
            {
                _stageWeaponPresentationProvider = provider;
                return _stageWeaponPresentationProvider != null && _stageWeaponPresentationProvider.BindingBridge != null && _stageWeaponPresentationProvider.BindingBridge.isActiveAndEnabled;
            }

            _stageWeaponPresentationProvider = null;
            return !OutGameStageControlRegistry.HasInstalledPresentationProviderInActiveScene() &&
                   !OutGameStageControlRegistry.HasInstalledBindingBridgeInActiveScene();
        }

        private bool TryEnsureCdcWeaponTouchControl()
        {
            if (!CanUseCdcWeaponPresentation())
            {
                return false;
            }

            if (_stageWeaponPresentationProvider == null)
            {
                return true;
            }

            if (_cdcWeaponTouchHandle != null && _cdcWeaponTouchHandle.IsValid)
            {
                return true;
            }

            OutGameStageControlResult result = _stageWeaponPresentationProvider.TryAcquireTouch(
                "CDC",
                _cdcWeaponTouchOwner,
                false,
                out StageControlHandle handle,
                out string reason);
            if (result != OutGameStageControlResult.Accepted)
            {
                Log.Debug($"[OutGameCharacterSystem] CDC weapon touch control unavailable. result={result}, reason={reason}");
                return false;
            }

            _cdcWeaponTouchHandle = handle;
            return true;
        }

        private bool TryEnsureCdcWeaponDissolveControl()
        {
            if (!CanUseCdcWeaponPresentation())
            {
                return false;
            }

            if (_stageWeaponPresentationProvider == null)
            {
                return true;
            }

            if (_cdcWeaponDissolveHandle != null && _cdcWeaponDissolveHandle.IsValid)
            {
                return true;
            }

            OutGameStageControlResult result = _stageWeaponPresentationProvider.TryAcquireDissolve(
                "CDC",
                _cdcWeaponDissolveOwner,
                false,
                out StageControlHandle handle,
                out string reason);
            if (result != OutGameStageControlResult.Accepted)
            {
                Log.Debug($"[OutGameCharacterSystem] CDC weapon dissolve control unavailable. result={result}, reason={reason}");
                return false;
            }

            _cdcWeaponDissolveHandle = handle;
            return true;
        }

        private bool TryEnsureCdcWeaponVisibilityControl()
        {
            if (!CanUseCdcWeaponPresentation())
            {
                return false;
            }

            if (_stageWeaponPresentationProvider == null)
            {
                return true;
            }

            if (_cdcWeaponVisibilityHandle != null && _cdcWeaponVisibilityHandle.IsValid)
            {
                return true;
            }

            OutGameStageControlResult result = _stageWeaponPresentationProvider.TryAcquireVisibility(
                "CDC",
                _cdcWeaponVisibilityOwner,
                false,
                out StageControlHandle handle,
                out string reason);
            if (result != OutGameStageControlResult.Accepted)
            {
                Log.Debug($"[OutGameCharacterSystem] CDC weapon visibility control unavailable. result={result}, reason={reason}");
                return false;
            }

            _cdcWeaponVisibilityHandle = handle;
            return true;
        }

        private bool TryApplyCdcWeaponVisibility(bool visible)
        {
            if (!CanUseCdcWeaponPresentation())
            {
                bool routerInstalled = OutGameStageControlRegistry.HasInstalledWeaponPresentationRouterInActiveScene();
                if (!routerInstalled || !visible)
                {
                    ApplyWeaponVisibilityInternal(visible);
                }

                return !routerInstalled;
            }

            if (_stageWeaponPresentationProvider == null)
            {
                ApplyWeaponVisibilityInternal(visible);
                return true;
            }

            if (!TryEnsureCdcWeaponVisibilityControl())
            {
                return false;
            }

            bool applied = false;
            _cdcWeaponVisibilityOwner.Handler = request =>
            {
                ApplyWeaponVisibilityInternal(request.Visible);
                applied = true;
                return true;
            };
            _stageWeaponPresentationProvider.TrySubmitVisibility(
                _cdcWeaponVisibilityHandle,
                new StageWeaponVisibilityRequest
                {
                    Visible = visible,
                    Source = nameof(TryApplyCdcWeaponVisibility)
                },
                out _);
            _cdcWeaponVisibilityOwner.Handler = null;
            return applied;
        }

        private void ReleaseCdcWeaponPresentationControl()
        {
            _cdcWeaponTouchOwner.Handler = null;
            _cdcWeaponDissolveOwner.Handler = null;
            _cdcWeaponTouchHandle?.Release();
            _cdcWeaponDissolveHandle?.Release();
            _cdcWeaponVisibilityHandle?.Release();
            _cdcWeaponTouchHandle = null;
            _cdcWeaponDissolveHandle = null;
            _cdcWeaponVisibilityHandle = null;
            _stageWeaponPresentationProvider = null;
        }

        private bool IsHandWeaponDissolvePlanStillValid(
            CharacterDetailWeaponDissolveTransitionPlan plan,
            int characterId,
            int tabIndex)
        {
            return plan != null &&
                   _handWeaponDissolveController.IsCurrent(plan) &&
                   plan.CharacterId == characterId &&
                   plan.TargetTabIndex == tabIndex &&
                   ResolveCurrentContext() == OutGameCharacterContext.Detail &&
                   Mathf.Max(0, _currentDetailTabIndex) == tabIndex &&
                   characterId == GetCurrentCharacterCfgId(OutGameCharacterContext.Detail);
        }

        private bool IsHandWeaponDissolvePlanStillValid(CharacterDetailWeaponDissolveTransitionPlan plan)
        {
            return plan != null &&
                   IsHandWeaponDissolvePlanStillValid(plan, plan.CharacterId, plan.TargetTabIndex);
        }

        private void TryScheduleHandWeaponDissolveIn(CharacterDetailWeaponDissolveTransitionPlan plan)
        {
            if (plan == null || !plan.IsEnter || !IsHandWeaponDissolvePlanStillValid(plan))
            {
                return;
            }

            if (!TryEnsureCdcWeaponDissolveControl())
            {
                if (_handWeaponInstance != null)
                {
                    WeaponDissolveAnchorV3Controller anchorController =
                        _handWeaponInstance.GetComponent<WeaponDissolveAnchorV3Controller>();
                    anchorController?.Initialize();
                    anchorController?.SetVisibleImmediate();
                }

                return;
            }

            if (_handWeaponInstance == null)
            {
                Log.Debug(
                    $"[OutGameCharacterSystem] Delay hand weapon PlayIn until weapon is ready. " +
                    $"characterId={plan.CharacterId}, from={plan.PreviousTabIndex}, to={plan.TargetTabIndex}.");
                return;
            }

            _cdcWeaponDissolveOwner.Handler = request =>
            {
                _handWeaponDissolveController.SchedulePlayIn(
                    plan,
                    () => _handWeaponInstance != null ? _handWeaponInstance.gameObject : null,
                    () => IsHandWeaponDissolvePlanStillValid(plan),
                    () => ResolveCurrentDetailTransitionActionSnapshot(plan.ActionContext));
                return true;
            };
            if (_stageWeaponPresentationProvider == null)
            {
                _cdcWeaponDissolveOwner.Handler(null);
            }
            else
            {
                _stageWeaponPresentationProvider.TrySubmitDissolve(
                    _cdcWeaponDissolveHandle,
                    new StageWeaponDissolveRequest { Operation = "PlayIn", CharacterId = plan.CharacterId, PreviousTabIndex = plan.PreviousTabIndex, TargetTabIndex = plan.TargetTabIndex },
                    out _);
            }
            _cdcWeaponDissolveOwner.Handler = null;
        }

        private void TryScheduleHandWeaponDissolveOut(CharacterDetailWeaponDissolveTransitionPlan plan)
        {
            if (plan == null || !plan.IsExit || !IsHandWeaponDissolvePlanStillValid(plan))
            {
                return;
            }

            if (!TryEnsureCdcWeaponDissolveControl())
            {
                HideCurrentHandWeaponDisplay();
                return;
            }

            int handWeaponLoadVersion = plan.HandWeaponLoadVersion > 0
                ? plan.HandWeaponLoadVersion
                : _handWeaponLoadVersion;
            _cdcWeaponDissolveOwner.Handler = request =>
            {
                _handWeaponDissolveController.SchedulePlayOut(
                    plan,
                    () => _handWeaponInstance != null ? _handWeaponInstance.gameObject : null,
                    () => IsHandWeaponDissolvePlanStillValid(plan),
                    () => HideCurrentHandWeaponDisplayIfVersion(handWeaponLoadVersion),
                    () => ResolveCurrentDetailTransitionActionSnapshot(plan.ActionContext));
                return true;
            };
            if (_stageWeaponPresentationProvider == null)
            {
                _cdcWeaponDissolveOwner.Handler(null);
            }
            else
            {
                _stageWeaponPresentationProvider.TrySubmitDissolve(
                    _cdcWeaponDissolveHandle,
                    new StageWeaponDissolveRequest { Operation = "PlayOut", CharacterId = plan.CharacterId, PreviousTabIndex = plan.PreviousTabIndex, TargetTabIndex = plan.TargetTabIndex },
                    out _);
            }
            _cdcWeaponDissolveOwner.Handler = null;
        }

        private CharacterDetailTransitionActionPlaybackSnapshot ResolveCurrentDetailTransitionActionSnapshot(
            CharacterDetailTransitionActionStartedContext context)
        {
            if (context == null || string.IsNullOrWhiteSpace(context.ActionState))
            {
                return default;
            }

            HallCharacterController hallCharacterController = GetCurrentHallCharacterController();
            if (hallCharacterController == null ||
                !hallCharacterController.TryGetAnimationPlaybackSnapshot(
                    context.ActionState,
                    out float normalizedTime,
                    out float playbackSpeed))
            {
                return default;
            }

            return new CharacterDetailTransitionActionPlaybackSnapshot
            {
                IsValid = true,
                NormalizedTime = normalizedTime,
                PlaybackSpeed = playbackSpeed
            };
        }

        private bool IsDetailHandWeaponRequestValid(
            int handWeaponLoadVersion,
            int characterCfgId,
            HallCharacterController hallCharacterController)
        {
            return handWeaponLoadVersion == _handWeaponLoadVersion &&
                   ResolveCurrentContext() == OutGameCharacterContext.Detail &&
                   IsDetailHandWeaponDissolveTab(hallCharacterController, characterCfgId, _currentDetailTabIndex) &&
                   characterCfgId == _displayedDetailCharacterCfgId &&
                   characterCfgId == GetCurrentCharacterCfgId(OutGameCharacterContext.Detail) &&
                   hallCharacterController != null &&
                   ReferenceEquals(hallCharacterController, GetCurrentHallCharacterController());
        }

        private bool IsDetailHandWeaponDissolveTab(
            HallCharacterController hallCharacterController,
            int characterCfgId,
            int tabIndex)
        {
            return hallCharacterController != null &&
                   _handWeaponDissolveController.IsVisibleTab(characterCfgId, hallCharacterController, tabIndex);
        }

        private void ConfigureHandWeaponDissolve(
            GameObject weaponGo,
            HallCharacterController hallCharacterController,
            CharacterDetailWeaponDissolveTransitionPlan dissolvePlan = null,
            bool forceVisibleWithoutDissolve = false)
        {
            if (weaponGo == null || hallCharacterController == null)
            {
                return;
            }

            if (!CanUseCdcWeaponPresentation())
            {
                return;
            }

            if (dissolvePlan != null &&
                dissolvePlan.Direction != CharacterDetailWeaponDissolveDirection.None)
            {
                if (dissolvePlan.IsEnter)
                {
                    _handWeaponDissolveController.PrepareEnterWeapon(weaponGo, dissolvePlan);
                }
                else
                {
                    _handWeaponDissolveController.ConfigureVisibleWeapon(weaponGo, dissolvePlan);
                }

                return;
            }

            bool wasActive = weaponGo.activeSelf;
            if (wasActive)
            {
                weaponGo.SetActive(false);
            }

            WeaponDissolveAnchorV3Controller anchorController = weaponGo.GetComponent<WeaponDissolveAnchorV3Controller>();
            if (anchorController == null)
            {
                Log.Warning(
                    $"[OutGameCharacterSystem] Hand weapon dissolve anchor missing " +
                    $"WeaponDissolveAnchorV3Controller. name={weaponGo.name}");
                if (wasActive)
                {
                    weaponGo.SetActive(true);
                }

                return;
            }

            anchorController.Initialize();
            if (forceVisibleWithoutDissolve)
            {
                anchorController.SetVisibleImmediate();
            }

            if (wasActive)
            {
                weaponGo.SetActive(true);
            }
        }

        private static bool ShouldForceHandWeaponVisibleOnRefresh(string reason)
        {
            return string.Equals(reason, "characterActivated", System.StringComparison.Ordinal);
        }

        private void HideDetailHandWeapon(
            HallCharacterController hallCharacterController,
            CharacterDetailWeaponDissolveTransitionPlan dissolvePlan = null)
        {
            int handWeaponLoadVersion = ++_handWeaponLoadVersion;
            if (dissolvePlan != null)
            {
                dissolvePlan.HandWeaponLoadVersion = handWeaponLoadVersion;
            }

            if (hallCharacterController == null)
            {
                hallCharacterController = GetCurrentHallCharacterController();
            }

            if (_handWeaponInstance != null)
            {
                if (!_handWeaponInstance.gameObject.activeInHierarchy)
                {
                    HideCurrentHandWeaponDisplayIfVersion(handWeaponLoadVersion);
                    return;
                }

                if (dissolvePlan != null && dissolvePlan.IsExit)
                {
                    _handWeaponDissolveController.ConfigureVisibleWeapon(_handWeaponInstance.gameObject, dissolvePlan);
                    if (dissolvePlan.RequiresCdcAction)
                    {
                        TryScheduleHandWeaponDissolveOut(dissolvePlan);
                        return;
                    }

                    TryScheduleHandWeaponDissolveOut(dissolvePlan);
                }
                else if (hallCharacterController != null)
                {
                    PlayCurrentHandWeaponDissolveOut(
                        hallCharacterController,
                        () => HideCurrentHandWeaponDisplayIfVersion(handWeaponLoadVersion));
                }
                else
                {
                    HideCurrentHandWeaponDisplay();
                }
            }
            else
            {
                HideCurrentHandWeaponDisplay();
            }

            // hallCharacterController?.SetEnableWeaponNodes(false);
        }

        private void PlayCurrentHandWeaponDissolveOut(
            HallCharacterController hallCharacterController,
            System.Action onCompleted)
        {
            if (!CanUseCdcWeaponPresentation() || _handWeaponInstance == null || hallCharacterController == null)
            {
                onCompleted?.Invoke();
                return;
            }

            WeaponDissolveAnchorV3Controller anchorController =
                _handWeaponInstance.GetComponent<WeaponDissolveAnchorV3Controller>();
            if (anchorController == null)
            {
                Log.Warning(
                    $"[OutGameCharacterSystem] Hand weapon dissolve anchor missing " +
                    $"WeaponDissolveAnchorV3Controller. name={_handWeaponInstance.name}");
                onCompleted?.Invoke();
                return;
            }

            anchorController.Initialize();
            if (!anchorController.PlayHide(
                0f,
                hallCharacterController.HandWeaponDissolveOutDuration,
                onCompleted))
            {
                onCompleted?.Invoke();
            }
        }

        private void HideCurrentHandWeaponDisplayIfVersion(int handWeaponLoadVersion)
        {
            if (handWeaponLoadVersion != _handWeaponLoadVersion)
            {
                return;
            }

            HideCurrentHandWeaponDisplay();
        }

        private void HideCurrentHandWeaponDisplay()
        {
            if (_handWeaponInstance != null)
            {
                _handWeaponInstance.gameObject.SetActive(false);
            }

            CharacterMainLightController.NotifyTargetsChanged();
        }

        private void ClearCurrentHandWeaponDisplay()
        {
            Transform previousHandWeaponRoot = _handWeaponRoot;
            Transform previousHandWeaponInstance = _handWeaponInstance;
            if (_characterLease != null &&
                previousHandWeaponInstance != null &&
                _characterLease.HandWeaponGo == previousHandWeaponInstance.gameObject)
            {
                _characterLease.HandWeaponGo = null;
                _characterLease.CurrentWeaponItemId = 0;
            }
            _handWeaponRoot = null;
            _handWeaponInstance = null;
            _currentHandWeaponItemId = 0;

            ReleasePreviousHandWeaponDisplay(
                previousHandWeaponRoot,
                previousHandWeaponInstance,
                nextHandWeaponRoot: null);

            CharacterMainLightController.NotifyTargetsChanged();
        }

        private void ReleasePreviousHandWeaponDisplay(
            Transform previousHandWeaponRoot,
            Transform previousHandWeaponInstance,
            Transform nextHandWeaponRoot)
        {
            if (previousHandWeaponRoot != null && previousHandWeaponRoot != nextHandWeaponRoot)
            {
                if (previousHandWeaponInstance != null)
                {
                    UnregisterCharacterMainLightTarget(previousHandWeaponInstance.gameObject);
                    Object.Destroy(previousHandWeaponInstance.gameObject);
                }

                Object.Destroy(previousHandWeaponRoot.gameObject);
                return;
            }

            if (previousHandWeaponInstance != null)
            {
                UnregisterCharacterMainLightTarget(previousHandWeaponInstance.gameObject);
                Object.Destroy(previousHandWeaponInstance.gameObject);
            }
        }

        private void ClearCurrentHandWeaponInstance()
        {
            if (_handWeaponInstance != null)
            {
                if (_characterLease != null &&
                    _characterLease.HandWeaponGo == _handWeaponInstance.gameObject)
                {
                    _characterLease.HandWeaponGo = null;
                    _characterLease.CurrentWeaponItemId = 0;
                }
                UnregisterCharacterMainLightTarget(_handWeaponInstance.gameObject);
                Object.Destroy(_handWeaponInstance.gameObject);
                _handWeaponInstance = null;
            }

            _currentHandWeaponItemId = 0;
        }

        private async UniTask SetWeaponDetail(int bagIndex, bool resetPoseIfSameWeapon = false, bool forceReload = false)
        {
            _weaponTouchController.SetActive(false);
            _weaponTouchController.CancelDrag();

            if (bagIndex < 0)
            {
                Log.Warning($"[OutGameCharacterSystem] bagIndex {bagIndex} 无效");
                return;
            }

            BagItemInfo bagItemInfo = BagDataModel.Instance.GetBagItemInfoByBagIndex(bagIndex);
            if (bagItemInfo == null)
            {
                Log.Warning($"[OutGameCharacterSystem] 未找到 BagIndex={bagIndex} 的背包物品");
                return;
            }

            var id = bagItemInfo.ItemId;

            Log.Debug($"[OutGameCharacterSystem] SetWeaponDetail(bagIndex={bagIndex}, id={id})");
            var weaponEquip = GameModule.Config.LubanTables.TbEquip.GetOrDefault(id);
            if (weaponEquip == null)
            {
                Log.Warning($"[OutGameCharacterSystem] 未找到 id 为 {id} 的武器");
                return;
            }

            if (_weaponAnchor == null)
            {
                _weaponAnchor = FindWeaponAnchorTransform(OutGameCharacterContext.Detail);
            }

            if (_weaponAnchor == null)
            {
                Log.Warning("[OutGameCharacterSystem] 未找到 WeaponMgr 节点");
                return;
            }

            if (!EnsureWeaponDisplayHierarchy())
            {
                Log.Warning("[OutGameCharacterSystem] 构建武器展示层级失败");
                return;
            }

            ResolveDetailWeaponCameras();

            if (!forceReload && _currentWeaponBagIndex == bagIndex && _weaponInstance != null)
            {
                Log.Debug($"[OutGameCharacterSystem] 武器已是相同 bagIndex={bagIndex}，跳过重新加载");
                _weaponReady = true;
                if (resetPoseIfSameWeapon)
                {
                    await _weaponTouchController.BindWeaponAsync(id, _weaponPivot, _weaponDisplayRoot, _weaponInstance, _detailWeaponCamera);
                }

                ApplyWeaponCharacterVisibility();
                return;
            }

            bool hasVisibleWeapon = _weaponInstance != null && _weaponReady;
            if (!hasVisibleWeapon)
            {
                _weaponReady = false;
                ApplyWeaponCharacterVisibility();
            }

            if (string.IsNullOrEmpty(weaponEquip.WeaponShowModel))
            {
                Log.Warning($"[OutGameCharacterSystem] 武器模型字段为空，EquipId={id}");
                return;
            }

            ClearStagedWeaponDisplay();

            int weaponLoadVersion = ++_weaponLoadVersion;
            string name = weaponEquip.WeaponShowModel;
            Log.Debug($"[OutGameCharacterSystem] 预加载武器 {name}");
            GameObject weaponGo = await GameModule.Resource.LoadGameObjectAsync(name, _weaponStagingRoot);
            if (weaponGo == null)
            {
                Log.Warning($"[OutGameCharacterSystem] 加载武器预制体失败: {name}");
                return;
            }

            if (weaponLoadVersion != _weaponLoadVersion || _weaponDisplayRoot == null || _weaponStagingRoot == null)
            {
                Object.Destroy(weaponGo);
                return;
            }

            Transform weaponTransform = weaponGo.transform;
            weaponGo.name = $"OutGameWeapon_{id}";
            weaponTransform.localPosition = Vector3.zero;
            weaponTransform.localRotation = Quaternion.identity;
            weaponTransform.localScale = Vector3.one;
            ApplyLayerToHierarchy(weaponGo, WeaponLayerName);

            _weaponReady = true;
            ReplaceCurrentWeaponDisplay(weaponTransform);
            _currentWeaponBagIndex = bagIndex;
            _currentWeaponItemId = id;
            await _weaponTouchController.BindWeaponAsync(id, _weaponPivot, _weaponDisplayRoot, _weaponInstance, _detailWeaponCamera);
            ApplyWeaponCharacterVisibility();
        }

        private async UniTask SetWeaponDetailByItemId(
            int id,
            int bagIndex,
            bool resetPoseIfSameWeapon = false,
            bool forceReload = false)
        {
            _weaponTouchController.SetActive(false);
            _weaponTouchController.CancelDrag();

            if (id <= 0)
            {
                Log.Warning($"[OutGameCharacterSystem] weapon item id {id} invalid");
                return;
            }

            Log.Debug($"[OutGameCharacterSystem] SetWeaponDetail(itemId={id}, bagIndex={bagIndex})");
            var weaponEquip = GameModule.Config.LubanTables.TbEquip.GetOrDefault(id);
            if (weaponEquip == null)
            {
                Log.Warning($"[OutGameCharacterSystem] weapon equip config missing, itemId={id}");
                return;
            }

            if (weaponEquip.ItemType != ItemType.Weapon)
            {
                Log.Warning($"[OutGameCharacterSystem] itemId={id} is not weapon equip");
                return;
            }

            if (_weaponAnchor == null)
            {
                _weaponAnchor = FindWeaponAnchorTransform(OutGameCharacterContext.Detail);
            }

            if (_weaponAnchor == null)
            {
                Log.Warning("[OutGameCharacterSystem] WeaponMgr node missing");
                return;
            }

            if (!EnsureWeaponDisplayHierarchy())
            {
                Log.Warning("[OutGameCharacterSystem] build weapon display hierarchy failed");
                return;
            }

            ResolveDetailWeaponCameras();

            if (!forceReload && _currentWeaponItemId == id && _weaponInstance != null)
            {
                Log.Debug($"[OutGameCharacterSystem] weapon itemId={id} already loaded, skip reload");
                _weaponReady = true;
                if (resetPoseIfSameWeapon)
                {
                    await _weaponTouchController.BindWeaponAsync(id, _weaponPivot, _weaponDisplayRoot, _weaponInstance, _detailWeaponCamera);
                }

                ApplyWeaponCharacterVisibility();
                return;
            }

            bool hasVisibleWeapon = _weaponInstance != null && _weaponReady;
            if (!hasVisibleWeapon)
            {
                _weaponReady = false;
                ApplyWeaponCharacterVisibility();
            }

            if (string.IsNullOrEmpty(weaponEquip.WeaponShowModel))
            {
                Log.Warning($"[OutGameCharacterSystem] weapon show model is empty, equipId={id}");
                return;
            }

            ClearStagedWeaponDisplay();

            int weaponLoadVersion = ++_weaponLoadVersion;
            string name = weaponEquip.WeaponShowModel;
            Log.Debug($"[OutGameCharacterSystem] preload weapon {name}");
            GameObject weaponGo = await GameModule.Resource.LoadGameObjectAsync(name, _weaponStagingRoot);
            if (weaponGo == null)
            {
                Log.Warning($"[OutGameCharacterSystem] load weapon prefab failed: {name}");
                return;
            }

            if (weaponLoadVersion != _weaponLoadVersion || _weaponDisplayRoot == null || _weaponStagingRoot == null)
            {
                Object.Destroy(weaponGo);
                return;
            }

            Transform weaponTransform = weaponGo.transform;
            weaponGo.name = $"OutGameWeapon_{id}";
            weaponTransform.localPosition = Vector3.zero;
            weaponTransform.localRotation = Quaternion.identity;
            weaponTransform.localScale = Vector3.one;
            ApplyLayerToHierarchy(weaponGo, WeaponLayerName);

            _weaponReady = true;
            ReplaceCurrentWeaponDisplay(weaponTransform);
            _currentWeaponBagIndex = bagIndex;
            _currentWeaponItemId = id;
            await _weaponTouchController.BindWeaponAsync(id, _weaponPivot, _weaponDisplayRoot, _weaponInstance, _detailWeaponCamera);
            ApplyWeaponCharacterVisibility();
        }

        public bool TryPlayCurrentCharacterLipSync(LipMouthShape shape)
        {
            RoomCharacterController roomCharacterController = GetCurrentRoomCharacterController();
            if (roomCharacterController == null)
            {
                return false;
            }

            return roomCharacterController.PlayLipSync(shape);
        }

        public void ClearCurrentCharacterLipSync()
        {
            RoomCharacterController roomCharacterController = GetCurrentRoomCharacterController();
            if (roomCharacterController == null)
            {
                return;
            }

            roomCharacterController.ClearLipSync();
        }
        private void ApplyWeaponCharacterVisibility()
        {
            if (_weaponAnchor == null)
            {
                _weaponAnchor = FindWeaponAnchorTransform(ResolveCurrentContext());
            }

            if (_characterRoot != null)
            {
                _characterRoot.gameObject.SetActive(!IsCharacterPresentationSuspended);
            }

            if (_characterGo != null)
            {
                _characterGo.SetActive(!_showWeapon);
            }

            CharacterMainLightController.NotifyTargetsChanged();
            bool weaponVisible = _showWeapon && _weaponReady;
            TryApplyCdcWeaponVisibility(weaponVisible);
        }

        private void ApplyWeaponVisibilityInternal(bool weaponVisible)
        {
            ApplyDetailWeaponCameraVisibility(weaponVisible);
            _weaponTouchController.SetActive(weaponVisible);

            if (_weaponPivot != null)
            {
                _weaponPivot.gameObject.SetActive(weaponVisible);
            }
        }

        private void UpdateCharacterWeaponModeVisibility()
        {
            if (_characterRoot != null)
            {
                _characterRoot.gameObject.SetActive(!IsCharacterPresentationSuspended);
                if (_characterGo != null)
                {
                    _characterGo.SetActive(!_showWeapon);
                }
            }

            if (!_showWeapon)
            {
                ResetDetailWeaponCameraVisibility();
                TryApplyCdcWeaponVisibility(false);

                CharacterMainLightController.NotifyTargetsChanged();
            }
        }

        private void ApplyDetailWeaponCameraVisibility(bool showWeaponCamera)
        {
            if (ResolveCurrentContext() != OutGameCharacterContext.Detail)
            {
                return;
            }

            ResolveDetailWeaponCameras();

            if (_detailMainCamera != null)
            {
                _detailMainCamera.enabled = !showWeaponCamera;
            }

            if (_detailWeaponCamera != null)
            {
                _detailWeaponCamera.enabled = showWeaponCamera;
            }
        }

        private void ResetDetailWeaponCameraVisibility()
        {
            ResolveDetailWeaponCameras();

            if (_detailMainCamera != null)
            {
                _detailMainCamera.enabled = true;
            }

            if (_detailWeaponCamera != null)
            {
                _detailWeaponCamera.enabled = false;
            }
        }

        private void ResolveDetailWeaponCameras()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!IsCameraInScene(_detailMainCamera, activeScene, DetailMainCameraName))
            {
                _detailMainCamera = FindCameraInScene(activeScene, DetailMainCameraName);
            }

            if (!IsCameraInScene(_detailWeaponCamera, activeScene, DetailWeaponCameraName))
            {
                _detailWeaponCamera = FindCameraInScene(activeScene, DetailWeaponCameraName);
            }
        }

        private void BindDetailDynamicCamera()
        {
            _stageBindingBridge = null;
            _stageCameraPresentationProvider = null;
            if (OutGameStageControlRegistry.TryGetActiveBindingBridge(out StagePresentationBindingBridge bindingBridge))
            {
                _stageBindingBridge = bindingBridge;
            }

            if (_stageBindingBridge != null &&
                _stageBindingBridge.TryResolveCamera(out Camera resolvedCamera, out _))
            {
                _detailMainCamera = resolvedCamera;
            }
            else if (OutGameStageControlRegistry.HasInstalledBindingBridgeInActiveScene())
            {
                _detailMainCamera = null;
                _detailCameraController.InvalidateSceneCamera();
                return;
            }
            else if (_module?.StageHostService != null &&
                     _module.StageHostService.TryGetCurrentBinder(out OutGameStageBinder binder) &&
                     binder != null &&
                     binder.StageKey == OutGameStageKey.CharacterDetail &&
                     binder.CameraRoot != null)
            {
                _detailMainCamera = FindCameraInRoot(binder.CameraRoot, DetailMainCameraName);
            }
            else
            {
                _detailMainCamera = null;
                _detailCameraController.InvalidateSceneCamera();
                return;
            }

            if (_detailMainCamera == null)
            {
                _detailCameraController.InvalidateSceneCamera();
                return;
            }

            if (_stageBindingBridge != null && _stageBindingBridge.TryGetProvider(OutGameStageControlChannel.Camera, out StageCameraPresentationProvider cameraProvider, out _))
            {
                OutGameStageControlResult result = cameraProvider.TryAcquire(
                    "CDC",
                    null,
                    false,
                    out StageControlHandle handle,
                    out string reason);
                if (result == OutGameStageControlResult.Accepted)
                {
                    _stageCameraPresentationProvider = cameraProvider;
                    _detailCameraController.BindCameraControlRouter(cameraProvider, handle);
                }
                else
                {
                    _detailCameraController.ClearCameraControlRouter();
                    Log.Debug($"[OutGameCharacterSystem] CDC camera control unavailable. result={result}, reason={reason}");
                }
            }
            else if (OutGameStageControlRegistry.HasInstalledCameraRouterInActiveScene())
            {
                _detailCameraController.ClearCameraControlRouter();
                _detailCameraController.InvalidateSceneCamera();
                Log.Debug("[OutGameCharacterSystem] CDC camera control blocked because the installed camera router is disabled.");
                return;
            }
            else
            {
                _detailCameraController.ClearCameraControlRouter();
            }

            _detailCameraController.SetSceneCamera(_detailMainCamera);
        }

        private bool TryEnsureCdcCharacterActionControl()
        {
            if (_stageBindingBridge == null)
            {
                OutGameStageControlRegistry.TryGetActiveBindingBridge(out _stageBindingBridge);
            }

            if (_stageBindingBridge == null || !_stageBindingBridge.TryGetProvider(OutGameStageControlChannel.CharacterAction, out StageCharacterActionPresentationProvider actionProvider, out _))
            {
                if (OutGameStageControlRegistry.HasInstalledBindingBridgeInActiveScene() ||
                    OutGameStageControlRegistry.HasInstalledCharacterActionRouterInActiveScene())
                {
                    _cdcCharacterActionHandle?.Release();
                    _cdcCharacterActionHandle = null;
                    _stageCharacterActionPresentationProvider = null;
                    return false;
                }

                _cdcCharacterActionHandle?.Release();
                _cdcCharacterActionHandle = null;
                _stageCharacterActionPresentationProvider = null;
                return true;
            }

            if (_cdcCharacterActionHandle != null && _cdcCharacterActionHandle.IsValid &&
                ReferenceEquals(_stageCharacterActionPresentationProvider, actionProvider))
            {
                return true;
            }

            _cdcCharacterActionHandle?.Release();
            OutGameStageControlResult result = actionProvider.TryAcquire(
                "CDC",
                _cdcCharacterActionOwner,
                false,
                out StageControlHandle handle,
                out string reason);
            if (result != OutGameStageControlResult.Accepted)
            {
                Log.Debug($"[OutGameCharacterSystem] CDC character action control unavailable. result={result}, reason={reason}");
                _cdcCharacterActionHandle = null;
                _stageCharacterActionPresentationProvider = actionProvider;
                return false;
            }

            _stageCharacterActionPresentationProvider = actionProvider;
            _cdcCharacterActionHandle = handle;
            return true;
        }

        private bool TryRunCdcCharacterAction(StageCharacterActionRequest request, System.Func<bool> operation)
        {
            if (!TryEnsureCdcCharacterActionControl())
            {
                return false;
            }

            if (_stageCharacterActionPresentationProvider == null)
            {
                return operation != null && operation();
            }

            _cdcCharacterActionOwner.Handler = requestToExecute => operation != null && operation();
            bool submitted = _stageCharacterActionPresentationProvider.TrySubmit(
                _cdcCharacterActionHandle,
                request,
                out string reason);
            _cdcCharacterActionOwner.Handler = null;
            if (!submitted)
            {
                Log.Debug($"[OutGameCharacterSystem] CDC character action rejected. reason={reason}");
            }

            return submitted;
        }

        private void CancelCdcCharacterAction()
        {
            if (_stageCharacterActionPresentationProvider == null &&
                (_stageBindingBridge == null || !_stageBindingBridge.TryGetProvider(
                    OutGameStageControlChannel.CharacterAction,
                    out StageCharacterActionPresentationProvider _,
                    out _)))
            {
                _detailActionController.CancelPendingAction();
                return;
            }

            if (!TryEnsureCdcCharacterActionControl())
            {
                return;
            }

            _cdcCharacterActionOwner.Handler = request =>
            {
                _detailActionController.CancelPendingAction();
                return true;
            };
            _stageCharacterActionPresentationProvider.TrySubmit(
                _cdcCharacterActionHandle,
                new StageCharacterActionRequest
                {
                    Cancel = true,
                    Source = nameof(CancelCurrentDetailPresentation)
                },
                out _);
            _cdcCharacterActionOwner.Handler = null;
        }

        private void ReleaseCdcCharacterActionControl()
        {
            _cdcCharacterActionOwner.Handler = null;
            _cdcCharacterActionHandle?.Release();
            _cdcCharacterActionHandle = null;
            _stageCharacterActionPresentationProvider = null;
        }

        private static Camera FindCameraInRoot(Transform root, string cameraName)
        {
            if (root == null || string.IsNullOrEmpty(cameraName))
            {
                return null;
            }

            Camera[] cameras = root.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (camera != null && camera.name == cameraName)
                {
                    return camera;
                }
            }

            return null;
        }

        private static bool IsCameraInScene(Camera camera, Scene scene, string cameraName)
        {
            return camera != null &&
                   camera.name == cameraName &&
                   scene.IsValid() &&
                   camera.gameObject.scene == scene;
        }

        private static Camera FindCameraInScene(Scene scene, string cameraName)
        {
            if (!scene.IsValid() || string.IsNullOrEmpty(cameraName))
            {
                return null;
            }

            GameObject[] rootGameObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootGameObjects.Length; i++)
            {
                Camera[] cameras = rootGameObjects[i].GetComponentsInChildren<Camera>(true);
                for (int j = 0; j < cameras.Length; j++)
                {
                    Camera camera = cameras[j];
                    if (camera != null && camera.name == cameraName)
                    {
                        return camera;
                    }
                }
            }

            return null;
        }

        private void ApplyAnimatorController(OutGameCharacterContext context, GameObject go, Animator animator)
        {
            _presentationService.TryApplyOutGameAnimatorController(
                go,
                context == OutGameCharacterContext.Lobby,
                nameof(OutGameCharacterSystem),
                out _);
        }
        private void ApplyAnimatorController(OutGameCharacterContext context, GameObject go, Animator animator, Character characterCfg)
        {
            RoomCharacterController hcc = ResolveRoomCharacterController(go);
            if (hcc == null) return;

            var controller = hcc.outGameAniController;
            
            if (controller == null)
            {
                Log.Warning($"[OutGameCharacterSystem] AnimatorController 加载失败: 角色预制体的局外AnimatorController未配置");
                return;
            }

            hcc.SetAniController(controller);
            animator.SetBool(ShowLobbyIdle, context == OutGameCharacterContext.Lobby);

            string hallName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(hallName))
            {
                hallName = context.ToString();
            }

            string characterName = ResolveInteractionCharacterName(characterCfg);
            if (string.IsNullOrEmpty(characterName))
            {
                Log.Warning($"[OutGameCharacterSystem] 无法初始化 HallInterActionConfig：角色名为空，CfgId={characterCfg?.Id}");
                return;
            }

            hcc.InitializeInteraction(hallName, characterName, string.Empty);
        }

        private void ApplyHouseholdInitialHallInteraction(GameObject go)
        {
            RoomCharacterController roomCharacterController = ResolveRoomCharacterController(go);
            if (roomCharacterController == null)
            {
                return;
            }

            HallInteraction hallInteraction = SelectRandomHouseholdInitialHallInteraction();
            if (hallInteraction == null)
            {
                Log.Warning("[OutGameCharacterSystem] 未找到可用的卧室初始交互配置，所有 HallInteraction.RandomWeight 均无效或配置表未加载。");
                return;
            }

            Log.Info($"[OutGameCharacterSystem] 随机选中卧室初始交互配置。Id={hallInteraction.Id}, RandomWeight={hallInteraction.RandomWeight}, InteractionObject={hallInteraction.InteractionObject}, InteractionPoint={hallInteraction.InteractionPoint}, ActionName={hallInteraction.ActionName}, IsRootmotion={hallInteraction.IsRootmotion}");
            roomCharacterController.TryApplyInitialHallInteraction(hallInteraction);
        }

        /// <summary>
        /// 选择卧室开场动作：优先从餐桌稳定 idle 中按权重随机，缺配置时保留旧的全表随机兜底。
        /// </summary>
        private HallInteraction SelectRandomHouseholdInitialHallInteraction()
        {
            List<HallInteraction> interactionList = GameModule.Config?.LubanTables?.TbHallInteraction?.DataList;
            if (interactionList == null || interactionList.Count == 0)
            {
                return null;
            }

            // 当前卧室默认期望角色坐在餐桌处，所以优先只在 DiningTable_Idle0~5 中随机。
            // 若配置缺失，再回退旧的全表权重选择，避免因为策划表异常导致角色完全不初始化。
            if (TrySelectRandomHouseholdInitialHallInteraction(interactionList, IsHouseholdInitialDiningTableIdle, out HallInteraction diningIdleInteraction))
            {
                return diningIdleInteraction;
            }

            Log.Warning("[OutGameCharacterSystem] 未找到餐桌 Idle0~Idle5 的卧室初始交互配置，回退为全表 RandomWeight 随机。");
            TrySelectRandomHouseholdInitialHallInteraction(interactionList, IsWeightedHouseholdInitialInteraction, out HallInteraction fallbackInteraction);
            return fallbackInteraction;
        }

        /// <summary>
        /// 在满足筛选条件的 HallInteraction 中按 RandomWeight 随机选择，复用旧权重语义。
        /// </summary>
        private static bool TrySelectRandomHouseholdInitialHallInteraction(IReadOnlyList<HallInteraction> interactionList,
            System.Func<HallInteraction, bool> predicate, out HallInteraction selectedInteraction)
        {
            selectedInteraction = null;
            if (interactionList == null || predicate == null)
            {
                return false;
            }

            int totalWeight = 0;
            for (int i = 0; i < interactionList.Count; i++)
            {
                HallInteraction interaction = interactionList[i];
                if (!predicate(interaction))
                {
                    continue;
                }

                totalWeight += interaction.RandomWeight;
            }

            if (totalWeight <= 0)
            {
                return false;
            }

            int randomWeight = Random.Range(0, totalWeight);
            int currentWeight = 0;
            for (int i = 0; i < interactionList.Count; i++)
            {
                HallInteraction interaction = interactionList[i];
                if (!predicate(interaction))
                {
                    continue;
                }

                currentWeight += interaction.RandomWeight;
                if (randomWeight < currentWeight)
                {
                    selectedInteraction = interaction;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// RandomWeight 大于 0 才允许进入初始候选池，和旧的卧室初始随机逻辑保持一致。
        /// </summary>
        private static bool IsWeightedHouseholdInitialInteraction(HallInteraction interaction)
        {
            return interaction != null && interaction.RandomWeight > 0;
        }

        /// <summary>
        /// 判断配置是否是本轮要求的餐桌开场 idle，防止随机到同对象的过渡动作。
        /// </summary>
        private static bool IsHouseholdInitialDiningTableIdle(HallInteraction interaction)
        {
            if (!IsWeightedHouseholdInitialInteraction(interaction))
            {
                return false;
            }

            // 餐桌初始动作只接受图中列出的稳定 idle，避免随机到 play 或过渡动作作为开场状态。
            return string.Equals(interaction.InteractionObject?.Trim() ?? string.Empty, HouseholdInitialDiningTableObjectName,
                       System.StringComparison.Ordinal)
                   && HouseholdInitialDiningTableIdleNames.Contains(interaction.ActionName?.Trim() ?? string.Empty)
                   && HasPlayableRelaxAction(interaction);
        }

        private static bool HasPlayableRelaxAction(HallInteraction interaction)
        {
            if (interaction?.ActionRelaxName == null)
            {
                return false;
            }

            for (int index = 0; index < interaction.ActionRelaxName.Count; index++)
            {
                if (!string.IsNullOrWhiteSpace(interaction.ActionRelaxName[index]))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ResolveInteractionCharacterName(Character characterCfg)
        {
            if (characterCfg == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(characterCfg.Name))
            {
                return characterCfg.Name;
            }

            if (!string.IsNullOrEmpty(characterCfg.NameLocal))
            {
                return characterCfg.NameLocal;
            }

            return characterCfg.Id.ToString();
        }

        private string ResolveHouseholdCharacterPrefabPath(Character characterCfg)
        {
            if (!string.IsNullOrEmpty(_debugHouseholdCharacterPrefabOverride))
            {
                return _debugHouseholdCharacterPrefabOverride;
            }

            if (characterCfg != null && !string.IsNullOrEmpty(characterCfg.BornSkin_Ref?.ShowModel))
            {
                return characterCfg.BornSkin_Ref.ShowModel;
            }

            return DefaultHouseholdCharacterPrefabPath;
        }

        private static string NormalizePrefabPath(string prefabPath)
        {
            return string.IsNullOrWhiteSpace(prefabPath)
                ? string.Empty
                : prefabPath.Trim().Replace("\\", "/");
        }

        private void ApplyContextComponents(
            GameObject go,
            Animator animator,
            OutGameCharacterContext context)
        {
            // Chat：心情/口型；Lobby/Detail 也可复用（无事件时不会触发）
            var chatModelMgr = go.GetComponent<ChatModelManager>();
            if (chatModelMgr == null)
            {
                chatModelMgr = go.AddComponent<ChatModelManager>();
            }
            chatModelMgr.Init(animator);
            chatModelMgr.SetEventsEnabled(context == OutGameCharacterContext.Chat);
            
            // 随机眨眼：所有局外场景都可用
            var blink = go.GetComponent<CharacterRandomEyeBlink>();
            if (blink == null)
            {
                blink = go.AddComponent<CharacterRandomEyeBlink>();
            }
            blink.Init(animator);
        }

        private static void ConfigureLobbyCharacterClickController(GameObject go, OutGameCharacterContext context)
        {
            HallCharacterController hallCharacterController = ResolveHallCharacterController(go);
            hallCharacterController?.SetLobbyClickEnabled(context == OutGameCharacterContext.Lobby);

            LobbyCharacterClickController clickController = go.GetComponent<LobbyCharacterClickController>();
            if (context != OutGameCharacterContext.Lobby)
            {
                if (clickController != null)
                {
                    clickController.enabled = false;
                }

                return;
            }

            if (clickController == null)
            {
                clickController = go.AddComponent<LobbyCharacterClickController>();
            }

            clickController.enabled = true;
            clickController.Initialize(go.transform);
        }

        private void EnsureRotationController(Transform anchor, OutGameCharacterContext context)
        {
            if (anchor == null)
            {
                return;
            }

            // 角色详情页不再允许滑动旋转角色，避免和详情页镜头/交互冲突；仅 Lobby 保持开启
            bool shouldEnable = context == OutGameCharacterContext.Lobby;

            var rotationController = anchor.GetComponent<PlayerRotationController>();
            if (!shouldEnable)
            {
                if (rotationController != null)
                {
                    rotationController.enabled = false;
                }
                return;
            }

            if (rotationController == null)
            {
                rotationController = anchor.gameObject.AddComponent<PlayerRotationController>();
            }

            rotationController.enabled = true;
            rotationController.SetTargetTransform(anchor);
            rotationController.SetMinDragDistance(20f);
            rotationController.SetInvertRotation(true);
        }

        private async UniTaskVoid HideWeaponInHandAsync(GameObject go)
        {
            var controller = ResolveHallCharacterController(go);
            if (controller != null)
            {
                // controller.SetEnableWeaponNodes(false);
            }

            await UniTask.Yield();
        }

        private Transform FindCharacterAnchorTransform(OutGameCharacterContext context)
        {
            if (_module != null &&
                _module.StageHostService != null &&
                _module.StageHostService.TryGetCurrentBinder(out OutGameStageBinder binder) &&
                binder != null &&
                binder.isActiveAndEnabled &&
                OutGameStageHostService.ToCharacterContext(binder.StageKey) == context &&
                binder.CharacterRoot != null)
            {
                return binder.CharacterRoot;
            }

            return FindAnchorTransformByCandidates(GetCharacterAnchorCandidates(context));
        }

        private Transform FindWeaponAnchorTransform(OutGameCharacterContext context)
        {
            if (_module != null &&
                _module.StageHostService != null &&
                _module.StageHostService.TryGetCurrentBinder(out OutGameStageBinder binder) &&
                binder != null &&
                binder.isActiveAndEnabled &&
                OutGameStageHostService.ToCharacterContext(binder.StageKey) == context &&
                binder.WeaponRoot != null)
            {
                return binder.WeaponRoot;
            }

            return FindAnchorTransformByCandidates(GetWeaponAnchorCandidates(context));
        }

        private Transform FindAnchorTransformByCandidates(string[] nodeNames)
        {
            if (nodeNames == null || nodeNames.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < nodeNames.Length; i++)
            {
                string nodeName = nodeNames[i];
                if (string.IsNullOrWhiteSpace(nodeName))
                {
                    continue;
                }

                Transform bindingTarget = TryFindAnchorTransform(nodeName);
                if (bindingTarget != null)
                {
                    return bindingTarget;
                }
            }

            return null;
        }

        private Transform TryFindAnchorTransform(string nodeName)
        {
            if (_module != null &&
                _module.StageHostService != null &&
                _module.StageHostService.TryGetCurrentBinder(out OutGameStageBinder binder) &&
                binder != null &&
                binder.TryResolveBinding(nodeName, out Transform bindingTarget))
            {
                return bindingTarget;
            }

            GameObject node = GameObject.Find(nodeName);
            if (node != null)
            {
                Log.Debug($"[OutGameCharacterSystem] node={nodeName} found, is {node}");
                return node.transform;
            }

            Log.Debug($"[OutGameCharacterSystem] node={nodeName} is null");
            return null;
        }

        private static string[] GetCharacterAnchorCandidates(OutGameCharacterContext context)
        {
            switch (context)
            {
                case OutGameCharacterContext.Detail:
                    return new[] { "DetailCharacterMgr", "CharacterMgr" };
                case OutGameCharacterContext.Lobby:
                case OutGameCharacterContext.Chat:
                case OutGameCharacterContext.Household:
                    return new[] { "CharacterMgr", "DetailCharacterMgr" };
                default:
                    return new[] { "CharacterMgr", "DetailCharacterMgr" };
            }
        }

        private static string[] GetWeaponAnchorCandidates(OutGameCharacterContext context)
        {
            switch (context)
            {
                case OutGameCharacterContext.Detail:
                    return new[] { "DetailWeaponMgr", "WeaponMgr" };
                default:
                    return new[] { "WeaponMgr", "DetailWeaponMgr" };
            }
        }

        private Transform GetOrCreateCharacterRoot(Transform anchor)
        {
            Transform root = anchor.Find(CharacterRootName);
            if (root != null)
            {
                ResetCharacterRootTransform(root);
                root.gameObject.SetActive(!IsCharacterPresentationSuspended);
                return root;
            }

            var rootGo = new GameObject(CharacterRootName);
            // 创建时先应用展示 gate，避免异步加载把新实例短暂挂到 active root 后再隐藏。
            rootGo.SetActive(!IsCharacterPresentationSuspended);
            rootGo.transform.SetParent(anchor, false);
            ResetCharacterRootTransform(rootGo.transform);
            return rootGo.transform;
        }

        private static void ResetCharacterRootTransform(Transform root)
        {
            if (root == null)
            {
                return;
            }

            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;
        }

        private bool IsCharacterPresentationSuspended => _presentationSuspensionRequestIds.Count > 0;

        /// <summary>
        /// 只切换 OutGame owner 创建的角色容器，不影响 CharacterMgr 下的 Camera、挂点或其他场景对象。
        /// 最后一个请求释放后恢复当前角色，并重新确保 Household 运行循环已启动。
        /// </summary>
        private void ApplyCharacterPresentationSuspension()
        {
            bool characterVisible = !IsCharacterPresentationSuspended;
            if (_characterRoot == null)
            {
                // 场景节点已就绪但角色系统尚未缓存 root 时，仍由 owner 主动解析自己的稳定容器。
                Transform characterAnchor = FindCharacterAnchorTransform(ResolveCurrentContext());
                _characterRoot = characterAnchor != null ? characterAnchor.Find(CharacterRootName) : null;
            }

            if (!characterVisible)
            {
                PreserveCurrentAnimatorStateForPresentationSuspension();
            }

            if (_characterRoot != null)
            {
                _characterRoot.gameObject.SetActive(characterVisible);
            }

            if (characterVisible)
            {
                // keepAnimatorStateOnDisable 必须覆盖 SetActive(false) -> true 的完整区间；
                // 角色重新激活后再还原资源原值，避免改变后续普通禁用语义。
                RestorePresentationSuspendedAnimatorSetting("PresentationResumed");
                if (_characterGo != null)
                {
                    EnsureHouseholdRuntimeStarted(ResolveCurrentContext(), _characterGo);
                }
            }

            CharacterMainLightController.NotifyTargetsChanged();
        }

        private void PreserveCurrentAnimatorStateForPresentationSuspension()
        {
            // 多个 suspension request 共享第一次隐藏前的快照，后续嵌套请求不能覆盖资源原值。
            if (_presentationSuspendedAnimator != null)
            {
                return;
            }

            Animator animator = _characterAnimator;
            if (animator == null && _characterGo != null)
            {
                animator = _characterGo.GetComponentInChildren<Animator>(true);
                _characterAnimator = animator;
            }

            if (animator == null)
            {
                return;
            }

            _presentationSuspendedAnimator = animator;
            _presentationSuspendedAnimatorOriginalKeepState = animator.keepAnimatorStateOnDisable;
            string animatorStateBefore = DescribePresentationAnimatorState(animator);
            animator.keepAnimatorStateOnDisable = true;

            Log.Info(
                $"[OutGameCharacterSystem] Preserved Animator for character presentation suspension. " +
                $"OriginalKeepState={_presentationSuspendedAnimatorOriginalKeepState}, State={animatorStateBefore}");
        }

        private void RestorePresentationSuspendedAnimatorSetting(string reason)
        {
            Animator animator = _presentationSuspendedAnimator;
            bool originalKeepState = _presentationSuspendedAnimatorOriginalKeepState;
            _presentationSuspendedAnimator = null;
            _presentationSuspendedAnimatorOriginalKeepState = false;

            if (animator == null)
            {
                return;
            }

            bool isCurrentAnimator = animator == _characterAnimator;
            string animatorStateAfter = DescribePresentationAnimatorState(animator);
            animator.keepAnimatorStateOnDisable = originalKeepState;

            Log.Info(
                $"[OutGameCharacterSystem] Restored Animator after character presentation suspension. " +
                $"Reason={reason}, IsCurrentAnimator={isCurrentAnimator}, RestoredKeepState={originalKeepState}, " +
                $"State={animatorStateAfter}");
        }

        private string DescribePresentationAnimatorState(Animator animator)
        {
            RoomCharacterController roomCharacterController = GetCurrentRoomCharacterController();
            string logicalAction = roomCharacterController != null &&
                                   roomCharacterController.TryGetCurrentActionName(out string actionName)
                ? actionName
                : string.Empty;

            if (animator == null)
            {
                return $"Animator=null, LogicalAction={logicalAction}";
            }

            string prefix =
                $"Animator={animator.name}#{animator.GetInstanceID()}, Enabled={animator.enabled}, " +
                $"ActiveInHierarchy={animator.gameObject.activeInHierarchy}, KeepState={animator.keepAnimatorStateOnDisable}, " +
                $"LogicalAction={logicalAction}";
            if (!animator.isActiveAndEnabled || animator.layerCount <= 0)
            {
                return prefix;
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
            string clipName = clipInfo != null && clipInfo.Length > 0 && clipInfo[0].clip != null
                ? clipInfo[0].clip.name
                : string.Empty;
            return
                $"{prefix}, FullPathHash={stateInfo.fullPathHash}, ShortNameHash={stateInfo.shortNameHash}, " +
                $"NormalizedTime={stateInfo.normalizedTime:F3}, Clip={clipName}";
        }

        private bool EnsureWeaponDisplayHierarchy()
        {
            if (_weaponAnchor == null)
            {
                _weaponAnchor = FindWeaponAnchorTransform(ResolveCurrentContext());
            }

            if (_weaponAnchor == null)
            {
                return false;
            }

            DisableAutoRotateOnHierarchy(_weaponAnchor);

            if (_weaponPivot == null)
            {
                _weaponPivot = _weaponAnchor.Find(WeaponTouchPivotName);
                if (_weaponPivot == null)
                {
                    var pivotGo = new GameObject(WeaponTouchPivotName);
                    pivotGo.transform.SetParent(_weaponAnchor, false);
                    pivotGo.transform.localPosition = Vector3.zero;
                    pivotGo.transform.localRotation = Quaternion.identity;
                    pivotGo.transform.localScale = Vector3.one;
                    ApplyLayerToHierarchy(pivotGo, WeaponLayerName);
                    _weaponPivot = pivotGo.transform;
                }
            }

            if (_weaponDisplayRoot == null)
            {
                Transform legacyRoot = _weaponAnchor.Find(LegacyWeaponRootName);
                Transform displayRoot = _weaponPivot.Find(WeaponDisplayRootName);
                if (displayRoot == null && legacyRoot != null)
                {
                    legacyRoot.SetParent(_weaponPivot, false);
                    legacyRoot.name = WeaponDisplayRootName;
                    displayRoot = legacyRoot;
                }

                if (displayRoot == null)
                {
                    var rootGo = new GameObject(WeaponDisplayRootName);
                    rootGo.transform.SetParent(_weaponPivot, false);
                    rootGo.transform.localPosition = Vector3.zero;
                    rootGo.transform.localRotation = Quaternion.identity;
                    rootGo.transform.localScale = Vector3.one;
                    ApplyLayerToHierarchy(rootGo, WeaponLayerName);
                    displayRoot = rootGo.transform;
                }

                _weaponDisplayRoot = displayRoot;
            }

            if (_weaponStagingRoot == null)
            {
                Transform stagingRoot = _weaponPivot.Find(WeaponStagingRootName);
                if (stagingRoot == null)
                {
                    var rootGo = new GameObject(WeaponStagingRootName);
                    rootGo.transform.SetParent(_weaponPivot, false);
                    rootGo.transform.localPosition = Vector3.zero;
                    rootGo.transform.localRotation = Quaternion.identity;
                    rootGo.transform.localScale = Vector3.one;
                    ApplyLayerToHierarchy(rootGo, WeaponLayerName);
                    stagingRoot = rootGo.transform;
                }

                stagingRoot.gameObject.SetActive(false);
                _weaponStagingRoot = stagingRoot;
            }

            ApplyLayerToHierarchy(_weaponPivot != null ? _weaponPivot.gameObject : null, WeaponLayerName);
            ApplyLayerToHierarchy(_weaponDisplayRoot != null ? _weaponDisplayRoot.gameObject : null, WeaponLayerName);
            ApplyLayerToHierarchy(_weaponStagingRoot != null ? _weaponStagingRoot.gameObject : null, WeaponLayerName);

            return _weaponPivot != null && _weaponDisplayRoot != null && _weaponStagingRoot != null;
        }

        private void ClearLoadedWeaponDisplay()
        {
            if (_weaponInstance != null)
            {
                UnregisterCharacterMainLightTarget(_weaponInstance.gameObject);
            }

            if (_weaponDisplayRoot != null)
            {
                HideAllChildren(_weaponDisplayRoot);
            }

            _weaponInstance = null;
        }

        private void ClearStagedWeaponDisplay()
        {
            if (_weaponStagingRoot != null)
            {
                HideAllChildren(_weaponStagingRoot);
            }
        }

        private void ReplaceCurrentWeaponDisplay(Transform stagedWeaponInstance)
        {
            if (_weaponDisplayRoot == null || stagedWeaponInstance == null)
            {
                return;
            }

            ClearLoadedWeaponDisplay();
            stagedWeaponInstance.gameObject.SetActive(true);
            stagedWeaponInstance.SetParent(_weaponDisplayRoot, false);
            stagedWeaponInstance.localPosition = Vector3.zero;
            stagedWeaponInstance.localRotation = Quaternion.identity;
            stagedWeaponInstance.localScale = Vector3.one;
            _weaponInstance = stagedWeaponInstance;
            RegisterCharacterMainLightTarget(stagedWeaponInstance.gameObject);
        }

        private static void ApplyLayerToHierarchy(GameObject root, string layerName)
        {
            if (root == null)
            {
                return;
            }

            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                Log.Warning($"[OutGameCharacterSystem] Layer `{layerName}` not found, skip layer assignment for {root.name}.");
                return;
            }

            SetLayerRecursively(root, layer);
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null)
            {
                return;
            }

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                transforms[i].gameObject.layer = layer;
            }
        }

        private static void RegisterCharacterMainLightTarget(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            CharacterMainLightController.RegisterTargetRoot(target.transform);
        }

        private static void UnregisterCharacterMainLightTarget(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            CharacterMainLightController.UnregisterTargetRoot(target.transform);
        }

        private void HideWeaponDisplayHierarchy()
        {
            _weaponTouchController.CancelDrag();

            if (_weaponInstance != null)
            {
                UnregisterCharacterMainLightTarget(_weaponInstance.gameObject);
            }

            if (_weaponPivot != null)
            {
                _weaponPivot.gameObject.SetActive(false);
            }
            _weaponInstance = null;
            CharacterMainLightController.NotifyTargetsChanged();
        }

        private static void DisableAutoRotateOnHierarchy(Transform root)
        {
            if (root == null)
            {
                return;
            }

            AutoRotate[] autoRotateComponents = root.GetComponentsInChildren<AutoRotate>(true);
            for (int i = 0; i < autoRotateComponents.Length; i++)
            {
                AutoRotate autoRotate = autoRotateComponents[i];
                if (autoRotate != null && autoRotate.enabled)
                {
                    autoRotate.enabled = false;
                }
            }
        }

        private void HideAllChildren(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child != null)
                {
                    UnregisterCharacterMainLightTarget(child.gameObject);
                    child.gameObject.SetActive(false);
                }
            }
        }

        private void PurgeLegacyOutGameCharacters(Transform anchor)
        {
            for (int i = anchor.childCount - 1; i >= 0; i--)
            {
                Transform child = anchor.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                // 我们自己的容器不删除，后续会清空其子节点
                if (child.name == CharacterRootName)
                {
                    continue;
                }

                bool looksLikeOutGameCharacter = !string.IsNullOrEmpty(child.name) &&
                                                child.name.StartsWith("OutGameCharacter_");
                if (!looksLikeOutGameCharacter && child.GetComponent<OutGameCharacterMarker>() == null &&
                    child.GetComponentInChildren<OutGameCharacterMarker>() == null)
                {
                    continue;
                }

                child.gameObject.SetActive(false);
            }
        }

        private void HideCurrentCharacterInstance()
        {
            if (_characterGo != null)
            {
                UnregisterCharacterMainLightTarget(_characterGo);
                _characterGo.SetActive(false);
            }

            ReleaseCharacterLease(_characterLease);
            _characterLease = null;

            _characterGo = null;
            _characterAnimator = null;
            _characterRoot = null;
        }

        private void ReleaseCharacterLease(OutGameCharacterPool.Lease lease)
        {
            if (lease == null)
            {
                return;
            }

            // UnityEngine.Object 的“已销毁”实例底层引用仍然非 null，不能使用
            // C# null-conditional 直接调用 GetComponent；否则从战斗返回大厅时，
            // 旧场景角色已被销毁但租约仍在，清理阶段会抛 MissingReferenceException，
            // 中断大厅 Stage 切换。
            GameObject characterGo = lease.CharacterGo;
            if (characterGo != null)
            {
                ChatModelManager chatModelManager = characterGo.GetComponent<ChatModelManager>();
                if (chatModelManager != null)
                {
                    chatModelManager.ResetState();
                }
            }

            _characterPool.Release(lease);
        }

        private void EnsureCharacterRootAttached(Transform characterRoot)
        {
            if (characterRoot == null || _characterGo == null)
            {
                return;
            }

            _characterRoot = characterRoot;
            ResetCharacterRootTransform(_characterRoot);
            _characterGo.transform.SetParent(_characterRoot, false);
        }

        private void ShowCharacterExclusively(Transform characterRoot, GameObject characterGo)
        {
            if (characterRoot == null || characterGo == null)
            {
                return;
            }

            HideCharacterSiblings(characterRoot, characterGo.transform);
            // child 保持 activeSelf，展示暂停期间仅关闭 owner root，释放后无需重建模型即可恢复。
            characterRoot.gameObject.SetActive(!IsCharacterPresentationSuspended);
            if (!characterGo.activeSelf)
            {
                characterGo.SetActive(true);
            }
            RegisterCharacterMainLightTarget(characterGo);
        }

        private void HideCharacterSiblings(Transform characterRoot, Transform visibleCharacter)
        {
            if (characterRoot == null || visibleCharacter == null)
            {
                return;
            }

            for (int i = characterRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = characterRoot.GetChild(i);
                if (child == null || child == visibleCharacter || visibleCharacter.IsChildOf(child))
                {
                    continue;
                }

                UnregisterCharacterMainLightTarget(child.gameObject);
                child.gameObject.SetActive(false);
            }
        }

        private async UniTask ApplyDetailPresentationAfterCharacterActivatedAsync(
            int characterCfgId,
            GameObject characterGo,
            int loadVersion = 0)
        {
            if (characterGo == null || ResolveCurrentContext() != OutGameCharacterContext.Detail)
            {
                return;
            }

            await _handWeaponDissolveController.EnsureConfigLoadedAsync();
            if (characterGo == null || ResolveCurrentContext() != OutGameCharacterContext.Detail)
            {
                return;
            }

            _detailCharacterVisualPrewarmScope.CaptureAndHide(characterGo.transform, _handWeaponRoot);
            int presentationVersion = ++_detailPresentationVersion;
            int tabIndex = Mathf.Max(0, _currentDetailTabIndex);
            try
            {
                await ApplyDefaultDetailEntryStateAsync(
                    characterCfgId,
                    preferImmediateAction: true,
                    allowDetailEnter: tabIndex == 0,
                    presentationVersion: presentationVersion);
            }
            finally
            {
                if (object.ReferenceEquals(characterGo, _characterGo))
                {
                    _detailCharacterVisualPrewarmScope.Restore();
                }
            }

            if (loadVersion > 0 && !IsCharacterLoadRequestStillValid(loadVersion, characterGo))
            {
                return;
            }

            ScheduleRefreshCurrentDetailHandWeapon("characterActivated");
        }

        private async UniTask<bool> TryActivateReusedDetailCharacterPairAsync(
            Transform characterRoot,
            int cfgId,
            OutGameCharacterContext context)
        {
            if (!TryFindReusableCharacterFromRoot(characterRoot, cfgId, context, out GameObject characterGo))
            {
                return false;
            }

            int loadVersion = ++_loadVersion;
            int handWeaponLoadVersion = ++_handWeaponLoadVersion;
            int preparedHandWeaponItemId = 0;
            Transform presentationStagingRoot = CreateDetailPresentationStagingRoot(characterRoot, loadVersion);
            UniTask<GameObject> handWeaponLoadTask = UniTask.FromResult<GameObject>(null);
            if (TryResolveCurrentDetailHandWeapon(cfgId, out preparedHandWeaponItemId, out string weaponModelPath))
            {
                handWeaponLoadTask = LoadDetailHandWeaponAnchorAsync(weaponModelPath, presentationStagingRoot);
            }

            GameObject preparedHandWeaponGo = await handWeaponLoadTask;
            if (loadVersion != _loadVersion ||
                context != ResolveCurrentContext() ||
                cfgId != GetCurrentCharacterCfgId(OutGameCharacterContext.Detail))
            {
                DestroyDetailPresentationStagingRoot(presentationStagingRoot);
                return true;
            }

            _characterGo = characterGo;
            _characterAnimator = characterGo.GetComponentInChildren<Animator>();
            _characterRoot = characterRoot;
            if (_characterAnimator == null)
            {
                Log.Error("[OutGameCharacterSystem] 复用角色模型缺少 Animator，初始化中止");
                DestroyPreparedDetailHandWeapon(preparedHandWeaponGo);
                DestroyDetailPresentationStagingRoot(presentationStagingRoot);
                return true;
            }

            CommitPreparedDetailHandWeapon(
                characterGo,
                cfgId,
                preparedHandWeaponItemId,
                preparedHandWeaponGo,
                handWeaponLoadVersion);
            DestroyDetailPresentationStagingRoot(presentationStagingRoot);
            ShowCharacterExclusively(characterRoot, characterGo);
            SetDisplayedDetailCharacter(context, cfgId);
            ConfigureLobbyCharacterClickController(characterGo, context);
            HideWeaponInHandAsync(characterGo).Forget();
            await ApplyDetailPresentationAfterCharacterActivatedAsync(cfgId, characterGo, loadVersion);
            ApplyWeaponCharacterVisibility();
            return true;
        }

        private bool TryReuseCharacterFromRoot(Transform characterRoot, int cfgId, OutGameCharacterContext context)
        {
            if (!TryFindReusableCharacterFromRoot(characterRoot, cfgId, context, out GameObject characterGo))
            {
                return false;
            }

            _characterGo = characterGo;
            _characterAnimator = _characterGo.GetComponentInChildren<Animator>();
            ShowCharacterExclusively(characterRoot, _characterGo);
            SetDisplayedDetailCharacter(context, cfgId);
            _characterRoot = characterRoot;
            ConfigureLobbyCharacterClickController(_characterGo, context);
            HideWeaponInHandAsync(_characterGo).Forget();
            return _characterAnimator != null;
        }

        private void SetDisplayedDetailCharacter(OutGameCharacterContext context, int cfgId)
        {
            if (context == OutGameCharacterContext.Detail)
            {
                _displayedDetailCharacterCfgId = cfgId;
            }
        }

        private static bool TryFindReusableCharacterFromRoot(
            Transform characterRoot,
            int cfgId,
            OutGameCharacterContext context,
            out GameObject characterGo)
        {
            characterGo = null;
            if (characterRoot == null)
            {
                return false;
            }

            for (int i = 0; i < characterRoot.childCount; i++)
            {
                Transform child = characterRoot.GetChild(i);
                if (child == null || !child.TryGetComponent(out OutGameCharacterMarker marker))
                {
                    continue;
                }

                if (marker.CharacterCfgId != cfgId || marker.Context != context)
                {
                    continue;
                }

                characterGo = child.gameObject;
                return true;
            }

            return false;
        }

        private int GetCurrentCharacterCfgId(OutGameCharacterContext context)
        {
            // 兼容旧逻辑：Characters（CharacterBrief）列表
            CharacterBrief current = CharacterDataModel.Instance.GetLobbyCurrentCharacter();
            if (context == OutGameCharacterContext.Detail)
            {
                if (CharacterDataModel.Instance.ViewModeCharacterCfgId > 0)
                {
                    return CharacterDataModel.Instance.ViewModeCharacterCfgId;
                }
                current = CharacterDataModel.Instance.GetDetailCurrentCharacter();
            }
            else if (context == OutGameCharacterContext.Household)
            {
                current = CharacterDataModel.Instance.GetHouseholdCurrentCharacter();
            }
            
            if (current != null)
            {
                return current.CfgId;
            }

            return 0;
        }

        private static OutGameCharacterPresentationProfile GetPresentationProfile(OutGameCharacterContext context)
        {
            switch (context)
            {
                case OutGameCharacterContext.Lobby:
                    return OutGameCharacterPresentationProfile.Lobby;
                case OutGameCharacterContext.Detail:
                    return OutGameCharacterPresentationProfile.Detail;
                case OutGameCharacterContext.Chat:
                    return OutGameCharacterPresentationProfile.Chat;
                default:
                    return OutGameCharacterPresentationProfile.Unknown;
            }
        }

        private static bool CanTransferPreviewContext(
            OutGameCharacterContext from,
            OutGameCharacterContext to)
        {
            return from != OutGameCharacterContext.Household &&
                   to != OutGameCharacterContext.Household;
        }

        private OutGameCharacterContext GetContextByActiveScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return OutGameCharacterContext.Unknown;
            }

            // 兼容不同命名（Map_ / Scenes_ 等）
            if (sceneName.Contains("GameLobby"))
            {
                return OutGameCharacterContext.Lobby;
            }

            if (sceneName.Contains("CharacterDetail"))
            {
                return OutGameCharacterContext.Detail;
            }
            

            if (sceneName.Contains("AIchat") || sceneName.Contains("Chat"))
            {
                if (sceneName.Contains("Bedroom"))
                    return OutGameCharacterContext.Household;
                return OutGameCharacterContext.Chat;
            }

            return OutGameCharacterContext.Unknown;
        }

        private OutGameCharacterContext ResolveCurrentContext()
        {
            if (_module != null)
            {
                OutGameCharacterContext context = _module.CurrentContext;
                if (context != OutGameCharacterContext.Unknown)
                {
                    return context;
                }
            }

            return GetContextByActiveScene(SceneManager.GetActiveScene().name);
        }

        internal static bool CanReuseCharacter(
            OutGameStageKey from,
            OutGameStageKey to,
            int currentCharacterCfgId,
            int targetCharacterCfgId)
        {
            if (currentCharacterCfgId <= 0 || targetCharacterCfgId <= 0)
            {
                return false;
            }

            if (currentCharacterCfgId != targetCharacterCfgId)
            {
                return false;
            }

            if (from == OutGameStageKey.Unknown || to == OutGameStageKey.Unknown)
            {
                return false;
            }

            if (from == to)
            {
                return true;
            }

            if (from == OutGameStageKey.Household || to == OutGameStageKey.Household)
            {
                return false;
            }

            return true;
        }

        private sealed class CdcCharacterActionOwner : IStageCharacterActionControlOwner
        {
            private readonly OutGameCharacterSystem _system;

            public CdcCharacterActionOwner(OutGameCharacterSystem system)
            {
                _system = system;
            }

            public System.Func<StageCharacterActionRequest, bool> Handler { get; set; }

            public bool TryHandleCharacterAction(StageCharacterActionRequest request)
            {
                return Handler != null && Handler(request);
            }

            public void OnCharacterActionControlRevoked(StageControlHandoff handoff)
            {
                Handler = null;
                if (_system == null)
                {
                    return;
                }

                _system._detailActionController.CancelPendingAction();
                _system._cdcCharacterActionHandle?.Release();
                _system._cdcCharacterActionHandle = null;
            }
        }

        private void StopCurrentHandWeaponDissolvePlayback()
        {
            if (_handWeaponInstance == null)
            {
                return;
            }

            WeaponDissolveAnchorV3Controller anchorController =
                _handWeaponInstance.GetComponent<WeaponDissolveAnchorV3Controller>();
            anchorController?.StopPlayback();
        }

        private sealed class CdcWeaponTouchOwner : IStageWeaponTouchControlOwner
        {
            private readonly OutGameCharacterSystem _system;
            public CdcWeaponTouchOwner(OutGameCharacterSystem system) { _system = system; }
            public System.Func<StageWeaponTouchRequest, bool> Handler { get; set; }
            public bool TryHandleWeaponTouch(StageWeaponTouchRequest request) => Handler != null && Handler(request);
            public void OnWeaponTouchControlRevoked(StageControlHandoff handoff)
            {
                Handler = null;
                if (_system == null)
                {
                    return;
                }

                _system._weaponTouchController.CancelDrag();
                _system._cdcWeaponTouchHandle?.Release();
                _system._cdcWeaponTouchHandle = null;
            }
        }

        private sealed class CdcWeaponDissolveOwner : IStageWeaponDissolveControlOwner
        {
            private readonly OutGameCharacterSystem _system;
            public CdcWeaponDissolveOwner(OutGameCharacterSystem system) { _system = system; }
            public System.Func<StageWeaponDissolveRequest, bool> Handler { get; set; }
            public bool TryHandleWeaponDissolve(StageWeaponDissolveRequest request) => Handler != null && Handler(request);
            public void OnWeaponDissolveControlRevoked(StageControlHandoff handoff)
            {
                Handler = null;
                if (_system == null)
                {
                    return;
                }

                _system._handWeaponDissolveController.CancelPending();
                _system.StopCurrentHandWeaponDissolvePlayback();
                _system._cdcWeaponDissolveHandle?.Release();
                _system._cdcWeaponDissolveHandle = null;
            }
        }

        private sealed class CdcWeaponVisibilityOwner : IStageWeaponVisibilityControlOwner
        {
            private readonly OutGameCharacterSystem _system;

            public CdcWeaponVisibilityOwner(OutGameCharacterSystem system)
            {
                _system = system;
            }

            public System.Func<StageWeaponVisibilityRequest, bool> Handler { get; set; }

            public bool TryHandleWeaponVisibility(StageWeaponVisibilityRequest request)
            {
                return Handler != null && Handler(request);
            }

            public void OnWeaponVisibilityControlRevoked(StageControlHandoff handoff)
            {
                Handler = null;
                if (_system == null)
                {
                    return;
                }

                _system._cdcWeaponVisibilityHandle?.Release();
                _system._cdcWeaponVisibilityHandle = null;
            }
        }
    }

    internal enum OutGameCharacterContext
    {
        Unknown = 0,
        Lobby = 1,
        Detail = 2,
        Chat = 3,
        Household = 4,
    }

    internal sealed class DetailCharacterVisualPrewarmScope
    {
        private readonly Dictionary<Renderer, bool> _rendererStates = new Dictionary<Renderer, bool>();

        /// <summary>
        /// 临时关闭角色本体 Renderer，等待相机和动作状态完成同步。
        /// </summary>
        /// <param name="root">需要预热的角色层级。</param>
        /// <param name="excludedRoot">由独立生命周期管理、不参与角色预热的子层级。</param>
        public void CaptureAndHide(Transform root, Transform excludedRoot = null)
        {
            Restore();
            if (root == null)
            {
                return;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || _rendererStates.ContainsKey(renderer))
                {
                    continue;
                }

                Transform rendererTransform = renderer.transform;
                if (excludedRoot != null &&
                    (rendererTransform == excludedRoot || rendererTransform.IsChildOf(excludedRoot)))
                {
                    continue;
                }

                _rendererStates.Add(renderer, renderer.enabled);
                if (renderer.enabled)
                {
                    renderer.enabled = false;
                }
            }
        }

        public void Restore()
        {
            foreach (KeyValuePair<Renderer, bool> pair in _rendererStates)
            {
                Renderer renderer = pair.Key;
                if (renderer == null)
                {
                    continue;
                }

                renderer.enabled = pair.Value;
            }

            _rendererStates.Clear();
        }
    }

    /// <summary>
    /// 运行时标记：用于判断当前实例是否需要重建。
    /// </summary>
    internal class OutGameCharacterMarker : MonoBehaviour
    {
        public int CharacterCfgId;
        public OutGameCharacterContext Context;
    }
}
