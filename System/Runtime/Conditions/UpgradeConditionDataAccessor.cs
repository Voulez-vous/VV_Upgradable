using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using VV.Utility;

namespace VV.Upgradable
{
    /// <summary>
    /// Component that provides access to upgrade condition data for scene components
    /// </summary>
    public class UpgradeConditionDataAccessor : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool sameAsCurrentUpgradable;
        [ConditionalHide("sameAsCurrentUpgradable")]
        [SerializeField] private UpgradableSO upgradableSO;
        [ConditionalHide("sameAsCurrentUpgradable")]
        [SerializeField] private UpgradableInstanceConfigSO instanceConfigSO;
        [SerializeField] [Min(0)] private int targetLevel;
        [SerializeField] private string dataKey = "";
        
        [Header("Events")]
        [SerializeField] private UnityEvent<int> OnIntValueChanged;
        [SerializeField] private UnityEvent<float> OnFloatValueChanged;
        [SerializeField] private UnityEvent<string> OnStringValueChanged;
        [SerializeField] private UnityEvent<object> OnValueChanged;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo;
        
        private UpgradableStateManager stateManager;
        private object currentValue;
        
        public object CurrentValue => currentValue;
        
        public T GetValue<T>()
        {
            if (currentValue is T typedValue)
                return typedValue;
            
            try
            {
                return (T)Convert.ChangeType(currentValue, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }
        
        public int GetIntValue() => GetValue<int>();
        public float GetFloatValue() => GetValue<float>();
        public string GetStringValue() => GetValue<string>();
        
        private void Start()
        {
            if (sameAsCurrentUpgradable)
            {
                AutoDetectUpgradableConfiguration();
            }
            
            if (upgradableSO == null)
            {
                Debug.LogError($"UpgradeConditionDataAccessor on {gameObject.name}: UpgradableSO is not assigned!", this);
                return;
            }
            
            // Get the state manager
            string id = instanceConfigSO ? instanceConfigSO.ID : upgradableSO.ID;
            
            if (UpgradeManager.UpgradableStateManagers.TryGetValue(id, out stateManager))
            {
                UpdateValue();
                // Subscribe to level changes if needed
                stateManager.LevelUped.AddListener(OnLevelChanged);
            }
            else
            {
                Debug.LogWarning($"UpgradeConditionDataAccessor on {gameObject.name}: Could not find state manager for {upgradableSO.name}", this);
            }
        }
        
        private void OnDestroy()
        {
            if (stateManager != null)
            {
                stateManager.LevelUped.RemoveListener(OnLevelChanged);
            }
        }
        
        private void OnLevelChanged(UpgradableStateManager sm)
        {
            UpdateValue();
        }
        
        private void UpdateValue()
        {
            if (upgradableSO == null) return;
            
            // Find the target level data
            UpgradableSO.UpgradableLevelData levelData = upgradableSO.LevelList.FirstOrDefault(l => l.Level == targetLevel);
            if (levelData == null)
            {
                if (showDebugInfo)
                    Debug.LogWarning($"UpgradeConditionDataAccessor: Level {targetLevel} not found in {upgradableSO.name}", this);
                return;
            }
            
            // Look for data providers in level conditions
            // If not found in level conditions, check global conditions
            var newValue = GetDataFromConditions(levelData.Conditions) ?? GetDataFromConditions(upgradableSO.GlobalConditions);

            if (newValue == null || newValue.Equals(currentValue)) return;
            currentValue = newValue;
            InvokeValueChangedEvents();
                
            if (showDebugInfo)
                Debug.Log($"UpgradeConditionDataAccessor: Value updated to {currentValue} for key '{dataKey}'", this);
        }
        
        private object GetDataFromConditions(List<UpgradeConditionSO> conditions)
        {
            if (conditions == null) return null;
            
            foreach (UpgradeConditionSO condition in conditions)
            {
                if (condition is not IUpgradeConditionDataProvider dataProvider) continue;
                if (dataProvider.HasData(dataKey))
                {
                    return dataProvider.GetData(dataKey);
                }
            }
            
            return null;
        }
        
        private void InvokeValueChangedEvents()
        {
            OnValueChanged?.Invoke(currentValue);
            
            // Invoke type-specific events
            if (currentValue is int intValue)
                OnIntValueChanged?.Invoke(intValue);
            else if (currentValue is float floatValue)
                OnFloatValueChanged?.Invoke(floatValue);
            else if (currentValue is string stringValue)
                OnStringValueChanged?.Invoke(stringValue);
            else
            {
                // Try to convert to common types
                try
                {
                    if (int.TryParse(currentValue.ToString(), out int convertedInt))
                        OnIntValueChanged?.Invoke(convertedInt);
                    else if (float.TryParse(currentValue.ToString(), out float convertedFloat))
                        OnFloatValueChanged?.Invoke(convertedFloat);
                    else
                        OnStringValueChanged?.Invoke(currentValue.ToString());
                }
                catch
                {
                    OnStringValueChanged?.Invoke(currentValue.ToString());
                }
            }
        }
        
        /// <summary>
        /// Manually refresh the value (useful for testing or external triggers)
        /// </summary>
        [ContextMenu("Refresh Value")]
        public void RefreshValue()
        {
            UpdateValue();
        }
        
        /// <summary>
        /// Get all available data keys from the current configuration
        /// </summary>
        public List<string> GetAvailableDataKeys()
        {
            var keys = new List<string>();
            
            if (upgradableSO == null) return keys;
            
            UpgradableSO.UpgradableLevelData levelData = upgradableSO.LevelList.FirstOrDefault(l => l.Level == targetLevel);
            if (levelData != null)
            {
                AddKeysFromConditions(levelData.Conditions, keys);
            }
            
            AddKeysFromConditions(upgradableSO.GlobalConditions, keys);
            
            return keys.Distinct().ToList();
        }
        
        private void AddKeysFromConditions(List<UpgradeConditionSO> conditions, List<string> keys)
        {
            if (conditions == null) return;
            
            foreach (UpgradeConditionSO condition in conditions)
            {
                if (condition is IUpgradeConditionDataProvider dataProvider)
                {
                    keys.AddRange(dataProvider.GetAvailableDataKeys());
                }
            }
        }
        
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
                
                if (showDebugInfo)
                {
                    Debug.Log($"UpgradeConditionDataAccessor: Auto-detected UpgradableSO '{upgradableSO?.name}' " +
                              $"and InstanceConfigSO '{instanceConfigSO?.name}' from {upgradableBase.name}", this);
                }
            }
            else
            {
                Debug.LogWarning($"UpgradeConditionDataAccessor on {gameObject.name}: " +
                                 "sameAsCurrentUpgradable is enabled but no UpgradableBase found in current GameObject or parents!", this);
            }
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-detect in editor when checkbox is toggled
            if (sameAsCurrentUpgradable && Application.isPlaying == false)
            {
                AutoDetectUpgradableConfiguration();
            }
        }
#endif
    }
}