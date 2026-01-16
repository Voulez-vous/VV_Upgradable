using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VV.Upgradable.Settings;
using Object = UnityEngine.Object;

namespace VV.Upgradable
{
    public static class UpgradeManager
    {
        public static Dictionary<string, UpgradableStateManager> UpgradableStateManagers { get; set; } = new();
        private static GameObject StateManagerContainer { get; set; }

        /// <summary>
        /// TODO: [WIP] UpgradeType is not currently used
        /// This enum should be used for global types but also 
        /// </summary>
        public static Dictionary<string, UpgradeType> UpgradeIdToType { get; set; } = new();

        #region Events
        
        // TODO : Create dictionaries of events with instance/upgradable type ids as key and event as value. e.g: Collectables
        
        /// <summary>
        /// Editor event (Optimisation not needed)
        /// </summary>
        public static event UnityAction<UpgradableStateManager> EditorLevelChanged;
        public static readonly Dictionary<string, UnityAction<UpgradableStateManager>> LevelChanged = new();
        public static readonly Dictionary<string, UnityAction<UpgradableStateManager>> MaxLevelReached = new();
        public static event UnityAction<UpgradableStateManager> MaxLevelAlreadyReached;
        public static event UnityAction<UpgradableStateManager> UpgradeSuccess;
        public static event UnityAction<UpgradableStateManager> UpgradeFailed;
        public static event UnityAction<UpgradableBase> UpgradableInitialized;
        public static event UnityAction<UpgradableStateManager> RollbackToPreviousLevel;
        public static event UnityAction UpgradableStateManagersInitialized;

        #endregion

        #region Init

        [RuntimeInitializeOnLoadMethod]
        public static void OnRuntimeInitialized()
        {
            try
            {
                UpgradableSettings customSettings = Resources.Load<UpgradableSettings>(UpgradableSettings.SettingsName);
                if(customSettings == null) return;
        
                StateManagerContainer = new GameObject("Upgradables");

                foreach (UpgradableSO upgradable in customSettings.activeUpgradables)
                {
                    CreateStateManager(upgradable);
                }
        
                Object.DontDestroyOnLoad(StateManagerContainer);
                UpgradableStateManagersInitialized?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"An error occured while initializing upgradables : {e.Message}");
                Debug.LogException(e);
            }
        }

        public static void CreateStateManager(UpgradableSO upgradable)
        {
            try
            {
                UpgradableStateManager newStateManager;
                
                string normalizedUpgradableName = upgradable.UpgradeName.Replace(" ", "");
                if (!Enum.TryParse(normalizedUpgradableName, out UpgradeType type))
                {
                    Debug.LogError($"Invalid upgradable name : {upgradable.UpgradeName}");
                    return;
                }

                if (upgradable.UpgradableInstances.Count == 0)
                {
                    if (upgradable.StateManagerPrefab)
                    {
                        GameObject upgradableStateManagerGo = Object.Instantiate(
                            upgradable.StateManagerPrefab, StateManagerContainer.transform);

                        newStateManager = upgradableStateManagerGo.GetComponent<UpgradableStateManager>();
                    }
                    else
                    {
                        newStateManager = GenerateStateManager(StateManagerContainer.transform, upgradable);
                    }

                    InitEvents(newStateManager);
                    UpgradableStateManagers.Add(upgradable.ID, newStateManager);
                }
                else
                {
                    GameObject subContainer = Object.Instantiate(
                        new GameObject($"{upgradable.UpgradeName}StateManagers"), StateManagerContainer.transform);
                    
                    foreach (UpgradableInstanceConfigSO instanceConfig in upgradable.UpgradableInstances)
                    {
                        GenerateStateManager(subContainer.transform, upgradable, instanceConfig, out newStateManager);
                        InitEvents(newStateManager);
                    }
                }
                UpgradeIdToType.Add(upgradable.ID, type);
            }
            catch (Exception e)
            {
                Debug.LogError($"An error occured while initializing upgradable {upgradable.UpgradeName} : {e.Message}");
                Debug.LogException(e);
            }
        }

        private static void GenerateStateManager(Transform parent, UpgradableSO upgradable,
            UpgradableInstanceConfigSO instanceConfig, out UpgradableStateManager stateManager)
        {
            stateManager = null;
            try
            {
                string normalizedInstanceName = instanceConfig.InstanceName.Replace(" ", "");
                if (!Enum.TryParse(normalizedInstanceName, out UpgradeType type))
                {
                    Debug.LogError($"Invalid instance name : {instanceConfig.InstanceName}");
                    return;
                }
                
                if (upgradable.StateManagerPrefab)
                {
                    GameObject newStateManagerGo = Object.Instantiate(upgradable.StateManagerPrefab, parent.transform);
            
                    stateManager = newStateManagerGo.GetComponent<UpgradableStateManager>();
                    stateManager.UpgradableInstanceSO = instanceConfig;
                } else
                {
                    GameObject newStateManagerGo = Object.Instantiate(
                        new GameObject($"{instanceConfig.InstanceName}StateManager"), parent.transform);
            
                    stateManager = newStateManagerGo.AddComponent<UpgradableStateManager>();
                    stateManager.UpgradableSO = upgradable;
                    stateManager.UpgradableInstanceSO = instanceConfig;
                }
                
                if (!stateManager.enabled)
                {
                    stateManager.enabled = true;
                }
                UpgradableStateManagers.Add(instanceConfig.ID, stateManager);
                UpgradeIdToType.Add(instanceConfig.ID, type);
            } catch (Exception e)
            {
                Debug.LogError($"[UpgradeManager] {upgradable.UpgradeName} instance {instanceConfig.InstanceName} Failed to initialize");
                Debug.LogException(e);
            }
        }

        private static void InitEvents(UpgradableStateManager stateManager)
        {
            // TODO : Do the same for all the events
            InitEventForSingleUpgradable(LevelChanged, stateManager);
            InitEventForSingleUpgradable(MaxLevelReached, stateManager);
        }

        private static void InitEventForSingleUpgradable(
            Dictionary<string, UnityAction<UpgradableStateManager>> eventsDictionary, 
            UpgradableStateManager stateManager)
        {
            if (!eventsDictionary.ContainsKey(stateManager.TypeID))
                eventsDictionary.Add(stateManager.TypeID, _ => {});
            if(stateManager.InstanceID != null && !eventsDictionary.ContainsKey(stateManager.InstanceID))
                eventsDictionary.Add(stateManager.InstanceID, _ => {});
        }

        private static UpgradableStateManager GenerateStateManager(Transform container, UpgradableSO upgradableSO)
        {
            string name = $"Upgradable{upgradableSO.UpgradeName}StateManager";
            
            GameObject upgradableStateManagerGo = Object.Instantiate(
                new GameObject(name), container);
                
            UpgradableStateManager upgradableStateManager = upgradableStateManagerGo.AddComponent<UpgradableStateManager>();
            upgradableStateManager.UpgradableSO = upgradableSO;

            return upgradableStateManager;
        }

        #endregion
        
        /// <summary>
        /// Force level update on the given upgradable.
        /// Useful for multiplayer purposes.
        /// </summary>
        /// <param name="upgradableId"></param>
        /// <param name="level"></param>
        public static void UpdateStateManager(string upgradableId, int level)
        {
            UpgradableStateManager stateManager = GetStateManagerById(upgradableId);
            stateManager.Level = level;
        }

        public static UpgradableStateManager GetStateManagerById(string upgradableId)
        {
            if(string.IsNullOrEmpty(upgradableId)) return null;

            UpgradableStateManagers.TryGetValue(upgradableId, out UpgradableStateManager stateManager);
            
            return stateManager;
        }

        #region Broadcast Events
        
        public static void BroadcastEditorLevelChanged(UpgradableStateManager upgradableStateManager)
        {
            EditorLevelChanged?.Invoke(upgradableStateManager);
        }

        public static void BroadcastLevelChanged(UpgradableStateManager upgradableStateManager)
        {
            if (LevelChanged == null)
            {
                Debug.LogWarning($"[UpgradeManager] LevelChanged event is null");
                return;
            }
            LevelChanged[upgradableStateManager.TypeID]?.Invoke(upgradableStateManager);
            if(upgradableStateManager.InstanceID != null)
                LevelChanged[upgradableStateManager.InstanceID]?.Invoke(upgradableStateManager);
        }

        public static void BroadcastMaxLevelReached(UpgradableStateManager upgradableStateManager)
        {
            if (MaxLevelReached == null)
            {
                Debug.LogWarning($"[UpgradeManager] MaxLevelReached event is null");
                return;
            }
            MaxLevelReached[upgradableStateManager.TypeID]?.Invoke(upgradableStateManager);
            if(upgradableStateManager.InstanceID != null)
                MaxLevelReached[upgradableStateManager.InstanceID]?.Invoke(upgradableStateManager);
        }
        
        public static void BroadcastMaxLevelAlreadyReached(UpgradableStateManager upgradableStateManager)
        {
            if (MaxLevelReached == null)
            {
                Debug.LogWarning($"[UpgradeManager] MaxLevelReached event is null");
                return;
            }
            MaxLevelReached[upgradableStateManager.TypeID]?.Invoke(upgradableStateManager);
            if(upgradableStateManager.InstanceID != null)
                MaxLevelReached[upgradableStateManager.InstanceID]?.Invoke(upgradableStateManager);
            MaxLevelAlreadyReached?.Invoke(upgradableStateManager);
        }
        
        public static void BroadcastLevelUp(UpgradableStateManager upgradableStateManager)
        {
            UpgradeSuccess?.Invoke(upgradableStateManager);
        }

        public static void BroadcastUpgradeFailed(UpgradableStateManager upgradableStateManager)
        {
            UpgradeFailed?.Invoke(upgradableStateManager);
        }

        public static void BroadcastInitialized(UpgradableBase upgradable)
        {
            UpgradableInitialized?.Invoke(upgradable);
        }
        
        public static void BroadcastLevelDown(UpgradableStateManager upgradableStateManager)
        {
            RollbackToPreviousLevel?.Invoke(upgradableStateManager);
        }
        #endregion
    }
}