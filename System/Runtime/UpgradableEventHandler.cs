using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VV.Logging;
using VV.Utility;
using Object = UnityEngine.Object;

namespace VV.Upgradable
{
    [Serializable]
    public class UpgradableEventHandler : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] protected bool sameAsCurrentUpgradable;
        [ConditionalHide("sameAsCurrentUpgradable")]
        [SerializeField] protected UpgradableSO upgradableSO;
        [ConditionalHide("sameAsCurrentUpgradable")]
        [SerializeField] protected UpgradableInstanceConfigSO instanceConfigSO;

        #region Events

        public UnityEvent<Object> OnUpgradeSuccess;
        public UnityEvent<Object> OnUpgradeFailed;
        public UnityEvent<int> OnEditorLevelChanged;
        public UnityEvent<int> OnTypeLevelChanged;
        public UnityEvent<int> OnInstanceLevelChanged;
        public UnityEvent<int> OnLevelMaxReached;
        public UnityEvent<int> OnLevelMax;
        public UnityEvent<int> OnLevelUp;
        public UnityEvent<object> OnLevelUpWithData;
        public UnityEvent<int> OnInitialized;
        public UnityEvent<int> OnLevelDown;

        #endregion

        #region Unity Events

        protected virtual void OnEnable()
        {
            this.LogLog($"[{GetType().Name}] OnEnable()");
            AutoDetectUpgradableConfiguration();
            UpgradeManager.EditorLevelChanged += EditorLevelChangedCallback;
            
            RegisterUpgradableEvent(UpgradeManager.LevelChanged,
                TypeLevelChangedCallback, InstanceLevelChangedCallback);
            RegisterUpgradableEvent(UpgradeManager.MaxLevelReached,
                LevelMaxReachedCallback, LevelMaxReachedCallback);
            
            // UpgradeManager.MaxLevelReached += LevelMaxReachedCallback;
            UpgradeManager.MaxLevelAlreadyReached += LevelMaxCallback;
            UpgradeManager.UpgradeSuccess += UpgradeSuccessCallback;
            UpgradeManager.UpgradeFailed += UpgradeFailedCallback;
            UpgradeManager.UpgradableInitialized += UpgradableInitializedCallback;
            UpgradeManager.RollbackToPreviousLevel += UpgradeManagerRollbackToPreviousLevel;
        }

        protected virtual void OnDisable()
        {
            this.LogLog($"[{GetType().Name}] OnDisable()");
            UpgradeManager.EditorLevelChanged -= EditorLevelChangedCallback;
            
            UnregisterUpgradableEvent(UpgradeManager.LevelChanged,
                TypeLevelChangedCallback, InstanceLevelChangedCallback);
            UnregisterUpgradableEvent(UpgradeManager.MaxLevelReached,
                LevelMaxReachedCallback, LevelMaxReachedCallback);
            
            // UpgradeManager.MaxLevelReached -= LevelMaxReachedCallback;
            UpgradeManager.MaxLevelAlreadyReached -= LevelMaxCallback;
            UpgradeManager.UpgradeSuccess -= UpgradeSuccessCallback;
            UpgradeManager.UpgradeFailed -= UpgradeFailedCallback;
            UpgradeManager.UpgradableInitialized -= UpgradableInitializedCallback;
        }

        protected void RegisterUpgradableEvent<T>(
            Dictionary<string, UnityAction<T>> broadcastEvent,
            UnityAction<T> typeAction, UnityAction<T> instanceAction)
        {
            broadcastEvent[upgradableSO.ID] += typeAction;
            if(instanceConfigSO)
                broadcastEvent[instanceConfigSO.ID] += instanceAction;
        }
        
        protected void UnregisterUpgradableEvent<T>(
            Dictionary<string, UnityAction<T>> broadcastEvent,
            UnityAction<T> typeAction, UnityAction<T> instanceAction)
        {
            broadcastEvent[upgradableSO.ID] -= typeAction;
            if(instanceConfigSO)
                broadcastEvent[instanceConfigSO.ID] -= instanceAction;
        }

        #endregion
        
        #region Callbacks
        
        private void EditorLevelChangedCallback(UpgradableStateManager stateManager)
        {
            Debug.Log($"[{GetType().Name}] {stateManager.Name} updated level to {stateManager.Level}");
            if (!stateManager.IsInstance(upgradableSO, instanceConfigSO)) return;
            OnEditorLevelChanged?.Invoke(stateManager.Level);
        }
        
        /// <summary>
        /// Only triggered when a single upgradable instance has it's level changed.
        /// </summary>
        /// <param name="stateManager"></param>
        private void InstanceLevelChangedCallback(UpgradableStateManager stateManager)
        {
            OnInstanceLevelChanged.Invoke(stateManager.Level);
        }
        
        /// <summary>
        /// Only triggered when an upgradable of the same type is triggered.
        /// </summary>
        /// <param name="stateManager"></param>
        private void TypeLevelChangedCallback(UpgradableStateManager stateManager)
        {
            OnTypeLevelChanged.Invoke(stateManager.Level);
        }

        private void LevelMaxReachedCallback(UpgradableStateManager stateManager)
        {
            Debug.Log($"[{GetType().Name}] {stateManager.Name} reached level max : {stateManager.Level}");
            if (!stateManager.IsInstance(upgradableSO, instanceConfigSO)) return;
            OnLevelMaxReached?.Invoke(stateManager.Level);
        }

        private void LevelMaxCallback(UpgradableStateManager stateManager)
        {
            Debug.Log($"[{GetType().Name}] {stateManager.Name} is level max : {stateManager.Level}");
            if (!stateManager.IsInstance(upgradableSO, instanceConfigSO)) return;
            OnLevelMax?.Invoke(stateManager.Level);
        }

        private void UpgradeSuccessCallback(UpgradableStateManager stateManager)
        {
            Debug.Log($"[{GetType().Name}] {stateManager.Name} successfully upgraded to level {stateManager.Level}");
            if (!stateManager.IsInstance(upgradableSO, instanceConfigSO)) return;
            OnLevelUp?.Invoke(stateManager.Level);
            OnLevelUpWithData?.Invoke(stateManager);
            OnUpgradeSuccess?.Invoke(stateManager);
        }

        private void UpgradeFailedCallback(UpgradableStateManager stateManager)
        {
            Debug.Log($"[{GetType().Name}] {stateManager.Name} failed to upgrade at level {stateManager.Level}");
            if (stateManager.IsInstance(upgradableSO, instanceConfigSO))
                OnUpgradeFailed?.Invoke(stateManager);
        }

        private void UpgradableInitializedCallback(UpgradableBase upgradable)
        {
            Debug.Log($"[{GetType().Name}] {upgradable.StateManager.Name} initialized at level {upgradable.StateManager.Level}");
            if(upgradable.StateManager.IsInstance(upgradableSO, instanceConfigSO))
                OnInitialized?.Invoke(upgradable.StateManager.Level);
        }
        
        private void UpgradeManagerRollbackToPreviousLevel(UpgradableStateManager stateManager)
        {
            Debug.Log($"[{GetType().Name}] {stateManager.Name} rollback to level {stateManager.Level}");
            if(stateManager.IsInstance(upgradableSO, instanceConfigSO))
                OnLevelDown?.Invoke(stateManager.Level);
        }

        #endregion

        #region Auto Detection

        /// <summary>
        /// Automatically detect upgradable configuration from current GameObject or parents
        /// </summary>
        private void AutoDetectUpgradableConfiguration()
        {
            // Try to find UpgradableBase in current GameObject or parents
            UpgradableBase upgradableBase = GetComponent<UpgradableBase>();
            if (upgradableBase == null)
                upgradableBase = GetComponentInParent<UpgradableBase>();
            
            if (upgradableBase != null)
            {
                upgradableSO = upgradableBase.UpgradableSO;
                instanceConfigSO = upgradableBase.InstanceConfigSO;
            }
            else
            {
                Debug.LogWarning($"UpgradableEventHandler on {gameObject.name}: " +
                                 "sameAsCurrentUpgradable is enabled but no UpgradableBase found in current GameObject or parents!", this);
            }
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-detect in editor when checkbox is toggled
            if (sameAsCurrentUpgradable && !Application.isPlaying)
            {
                AutoDetectUpgradableConfiguration();
            }
        }
#endif

        #endregion
    }
}