using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using VV.Utility;

namespace VV.Upgradable
{
    public class UpgradableStateManager : MonoBehaviour
    {
        #region Variables

        [SerializeField] protected UpgradableSO upgradableSO;
        [Tooltip("This value is set only if multiple instances can be upgraded.")]
        [SerializeField] [ReadOnly] protected UpgradableInstanceConfigSO upgradableInstanceSO;
        
        [SerializeField] protected int level;

        public UpgradableSO UpgradableSO
        {
            get => upgradableSO;
            set
            {
                upgradableSO = value;
                OnUpgradableSOSet();
            } 
        }

        public UpgradableInstanceConfigSO UpgradableInstanceSO
        {
            get => upgradableInstanceSO;
            set
            {
                upgradableInstanceSO = value;
                OnUpgradableInstanceConfigSOSet();
            } 
        }

        public string TypeID => upgradableSO.ID;
        public string InstanceID => UpgradableInstanceSO == null ? null : UpgradableInstanceSO.ID;
        public string Name => upgradableSO.UpgradeName;

        public int Level
        {
            get => level;
            set {
                level = value;
                OnLevelChanged();
            }
        }

        #endregion

        #region Events

        public UnityEvent<UpgradableStateManager> LevelUped = new();
        public UnityEvent<UpgradableStateManager> LevelDowned = new();
        
        #endregion
        
        #region Unity Callbacks

        protected virtual void Awake()
        {
            UpgradableSO?.Init();
        }

        protected virtual void Start()
        {
            if(UpgradableInstanceSO && !string.IsNullOrEmpty(UpgradableInstanceSO.InstanceName))
                name = $"{UpgradableInstanceSO.InstanceName}_StateManager";
            // Debug.Log($"Upgradable {Name} started");
        }

        #endregion

        #region Upgrade Logic

        public virtual void LevelUp()
        {
            // Debug.Log($"{upgradableSO.UpgradeName}'s Level up reached");
            if (Level == UpgradableSO.MaxLevel)
            {
                // Debug.Log($"{upgradableSO.UpgradeName}'s Max Level already reached");
                UpgradeManager.BroadcastMaxLevelAlreadyReached(this);
                return;
            }
            
            if (CanLevelUp())
            {
                // Debug.Log($"{upgradableSO.UpgradeName}'s Level up reached");
                Level++;
                OnLevelUp();
                UpgradeManager.BroadcastLevelUp(this);
                if (Level == UpgradableSO.MaxLevel)
                    UpgradeManager.BroadcastMaxLevelReached(this);
            }
            else UpgradeManager.BroadcastUpgradeFailed(this);
        }
        
        public virtual void LevelDown()
        {
            if (Level == 0)
            {
                // TODO : Maybe broadcast cant level down (level Min Reached)
                return;
            }
            
            Level--;
            OnLevelDown();

            UpgradeManager.BroadcastLevelDown(this);

            // TODO : Maybe broadcast level down
        }

        /// <summary>
        /// The default behaviour if the object has no conditions for a specific level is to accept the upgrade.
        /// </summary>
        /// <returns></returns>
        protected virtual bool CanLevelUp()
        {
            if (Level >= upgradableSO.MaxLevel)
                return false;
            bool canUpgrade = upgradableSO.CanUpgrade(Level);
            // Debug.Log($"Upgradable {upgradableSO.UpgradeName} CanUpgrade={canUpgrade}");
            return canUpgrade;
        }
        
        protected virtual bool CanLevelUp(int levelToTest)
        {
            if (levelToTest >= upgradableSO.MaxLevel)
                return false;
            bool canUpgrade = upgradableSO.CanUpgrade(levelToTest);
            // Debug.Log($"Upgradable {upgradableSO.UpgradeName} CanUpgrade={canUpgrade}");
            return canUpgrade;
        }

        protected virtual void OnLevelUp()
        {
            LevelUped?.Invoke(this);
        }
        protected virtual void OnLevelDown()
        {
            LevelDowned?.Invoke(this);
        }

        protected virtual void OnLevelChanged()
        {
            UpgradeManager.BroadcastLevelChanged(this);
            if(Level == upgradableSO.MaxLevel) UpgradeManager.BroadcastMaxLevelReached(this);
        }
        
        protected virtual void OnUpgradableSOSet() { }

        protected virtual void OnUpgradableInstanceConfigSOSet() { }
        
        #endregion

        /// <summary>
        /// Checks if the target is bounded to the current StateManager.
        /// </summary>
        /// <param name="targetUpgradableSO"></param>
        /// <param name="targetUpgradableInstanceSO"></param>
        /// <returns></returns>
        public bool IsInstance(UpgradableSO targetUpgradableSO, UpgradableInstanceConfigSO targetUpgradableInstanceSO = null)
        {
            if (!targetUpgradableSO || 
                (targetUpgradableInstanceSO && !upgradableInstanceSO)) 
                return false;

            bool isInstance = !targetUpgradableInstanceSO
                ? UpgradableSO.Equals(targetUpgradableSO)
                : upgradableInstanceSO.Equals(targetUpgradableInstanceSO);
            
            // Debug.Log($"[{GetType().Name}] {targetUpgradableInstanceSO?.InstanceName} is instance of {UpgradableSO?.UpgradeName}'s {upgradableInstanceSO?.InstanceName} ? {isInstance}");
            //
            // Debug.Log($"[{GetType().Name}] {UpgradableSO?.UpgradeName} ({UpgradableSO?.ID}) Equals {targetUpgradableSO?.UpgradeName} ({targetUpgradableSO?.ID}) ? {UpgradableSO?.Equals(targetUpgradableSO)}");
            // Debug.Log($"{string.Join("", UpgradableSO?.ID.Except(targetUpgradableSO?.ID))}");
            // Debug.Log($"[{GetType().Name}] {upgradableInstanceSO?.InstanceName} ({upgradableInstanceSO?.ID}) Equals {targetUpgradableInstanceSO?.InstanceName} ({targetUpgradableInstanceSO?.ID}) ? {upgradableInstanceSO?.Equals(targetUpgradableInstanceSO)}");
            // if (targetUpgradableInstanceSO?.ID != null)
            //     Debug.Log($"{string.Join("", upgradableInstanceSO?.ID.Except(targetUpgradableInstanceSO?.ID))}");
            return isInstance;
        }

        public bool IsLevelMax()
        {
            return Level == UpgradableSO.MaxLevel;
        }

        /// <summary>
        /// TODO : Do something similar with the score configs to replace it
        /// Gets the score set in the upgradable SO according to the current upgradable's level.
        /// </summary>
        /// <param name="cumulative"></param>
        /// <returns></returns>
        public int GetCurrentLevelScore(bool cumulative = false)
        {
            return cumulative ? 
                upgradableSO.LevelList.Sum(levelData => levelData.Level <= Level ? levelData.Score : 0) : 
                upgradableSO.LevelList[level].Score;
        }
        
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if(!upgradableSO) return;
            
            Level = level > upgradableSO.MaxLevel ? upgradableSO.MaxLevel : level;
            UpgradeManager.BroadcastEditorLevelChanged(this);
        }

        [Button("Level Up", engine = AttributeEngine.UIToolkit)]
        protected virtual void EditorLevelUp()
        {
            LevelUp();
        }
#endif
    }
}