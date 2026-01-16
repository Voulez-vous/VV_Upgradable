using UnityEngine;
using UnityEngine.SceneManagement;
using VV.Utility;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VV.Upgradable
{
    public class UpgradableBase : MonoBehaviour
    {
        #region Variables

        [SerializeField] protected UpgradableSO upgradableSO;
        [SerializeField] protected UpgradableInstanceConfigSO instanceConfigSO;
        
        [SerializeField] [ReadOnly] protected UpgradableStateManager stateManager;

        public UpgradableSO UpgradableSO => upgradableSO;

        public UpgradableInstanceConfigSO InstanceConfigSO => instanceConfigSO;

        public UpgradableStateManager StateManager => stateManager;

        #endregion
        
        protected virtual void Start()
        {
            if(instanceConfigSO == null) {
                UpgradeManager.UpgradableStateManagers.TryGetValue(upgradableSO.ID, out stateManager);
            }
            else
            {
                UpgradeManager.UpgradableStateManagers.TryGetValue(instanceConfigSO.ID, out stateManager);
            }
            
            enabled = stateManager != null;

            if (!stateManager) return;
            
            UpgradeManager.UpgradeSuccess += OnUpgradeSuccess;
            UpgradeManager.BroadcastInitialized(this);
        }

        public virtual void Upgrade()
        {
            // Debug.Log($"Trying to upgrade {upgradableSO.UpgradeName} : stateManager={stateManager?.Name} - enabled={enabled}");
            if (stateManager == null || !enabled) return;
            
            stateManager.LevelUp();
        }

        protected virtual void OnUpgradeSuccess(UpgradableStateManager sm) { }

#if UNITY_EDITOR
        public void GenerateInstanceConfigSO()
        {
            if(UpgradableSO == null || InstanceConfigSO != null) return;

            UpgradableInstanceConfigSO foundConfig = UpgradableSO.FindUpgradableInstanceConfigData(this);
            if (foundConfig)
            {
                instanceConfigSO = foundConfig;
                return;
            }
            
            UpgradableInstanceConfigSO newConfigSO = ScriptableObject.CreateInstance<UpgradableInstanceConfigSO>();
            newConfigSO.SceneName = SceneManager.GetActiveScene().name;
            instanceConfigSO = UpgradableSO.StoreConfigData(newConfigSO);
            SetInstanceName();

            Save();
        }

        public virtual void SetInstanceName()
        {
            instanceConfigSO.SetSceneName(SceneManager.GetActiveScene().name);
        }

        protected void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
#endif
    }
}