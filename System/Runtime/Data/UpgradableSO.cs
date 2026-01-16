using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using VV.SO;
using VV.Utility;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using VV.Upgradable.Settings;
#endif

namespace VV.Upgradable
{
    [CreateAssetMenu(menuName = "VV/UpgradableObjects/UpgradableSO", fileName = "UpgradableSO")]
    public class UpgradableSO : SerializableScriptableObject
    {
        #region Variables
        public string ID => VVGuid.ToString();
        
        [SerializeField] private string upgradeName;
        [SerializeField] private int maxLevel = 1;
        
        [SerializeField] private GameObject stateManagerPrefab;
        
        [Tooltip("Each data should refer to a unique upgradable object.")]
        [SerializeField] private List<UpgradableInstanceConfigSO> upgradableInstances = new();
        
        [Tooltip("Each SO should implement IUpgradeCondition.")]
        [SerializeField] private List<UpgradeConditionSO> globalConditions;
        [Tooltip("The scores for each level up. Each upgradable starts at level 0.")]
        [SerializeField] private List<UpgradableLevelData> levelList;

        /// <summary>
        /// TODO : [WIP] use this boolean to create a unique instance config for upgradables used in every scenes (e.g: Portal)
        /// </summary>
        [SerializeField] private bool useSharedInstanceConfig;
        
        [Serializable]
        public class UpgradableLevelData
        {
            [SerializeField] [HideInInspector] private string name; 
            public void SetName() => name = $"Level {level}";
            [SerializeField] private int level;
            [Tooltip("Score earned after upgrading to this level.")]
            [SerializeField] [Obsolete("THe score should not be in this component")] private int score;
            [Tooltip("Conditions to upgrade to any level.")]
            [SerializeField] private List<UpgradeConditionSO> conditions;

            public string Name => name;
            public int Level => level;
            public int Score => score;
            [Tooltip("Conditions to upgrade to this level.")]
            public List<UpgradeConditionSO> Conditions => conditions;
        }

        public string UpgradeName => upgradeName;

        public int MaxLevel => maxLevel;

        public bool UseSharedInstanceConfig => useSharedInstanceConfig;

        public List<UpgradeConditionSO> GlobalConditions => globalConditions;

        public GameObject StateManagerPrefab => stateManagerPrefab;

        public List<UpgradableLevelData> LevelList => levelList;

        public List<UpgradableInstanceConfigSO> UpgradableInstances
        {
            get => upgradableInstances;
            set => upgradableInstances = value;
        }

        #endregion

        public void Init()
        {
            // Debug.Log($"Upgradable {UpgradeName}SO Init...");
            globalConditions.ForEach(condition => condition.Init());
            
            levelList.ForEach(level => level.Conditions.ForEach(condition => condition.Init()));
            // Debug.Log($"Upgradable {UpgradeName}SO Init successful");
        }

        /// <summary>
        /// The default behaviour if the object has no conditions is to accept the upgrade.
        /// </summary>
        /// <returns></returns>
        public bool CanUpgrade(int level)
        {
            // Debug.Log($"Upgradable {UpgradeName}SO CanUpgrade {level}...");
            UpgradableLevelData currentLevelData = LevelList.FirstOrDefault(levelData => levelData.Level == level+1);
            
            if(currentLevelData == null) return false;
            
            foreach (UpgradeConditionSO condition in GlobalConditions)
            {
                if (!condition.CanUpgrade())
                    return false;
            }

            foreach (UpgradeConditionSO condition in currentLevelData.Conditions)
            {
                if(!condition.CanUpgrade())
                    return false;
            }

            return true;
        }

        protected bool Equals(UpgradableSO other)
        {
            return string.Equals(ID, other.ID);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((UpgradableSO)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), ID);
        }

#if UNITY_EDITOR
        
        [Header("Settings")]
        public string CurrentFolder {
            get
            {
                string[] folders = AssetDatabase.GetAssetPath(this).Split('/');
                folders = folders.Take(folders.Length - 1).ToArray();
                return String.Join('/', folders);
            }
        }
        public string FolderInstancePath => Path.Combine(CurrentFolder, $"{upgradeName} Instance");
        public string AssetPath => AssetDatabase.GenerateUniqueAssetPath(Path.Combine(CurrentFolder, $"{upgradeName} Instance Config.asset"));

        #region Unity Functions

        protected override void Awake()
        {
            base.Awake();
            if (!String.IsNullOrEmpty(ID)) return;
            UpgradableSettings customSettings = Resources.Load<UpgradableSettings>(UpgradableSettings.SettingsName);
            customSettings.activeUpgradables.AddUnique(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UpgradableSettings customSettings = Resources.Load<UpgradableSettings>(UpgradableSettings.SettingsName);
            customSettings.activeUpgradables.Remove(this);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            foreach (UpgradableLevelData level in LevelList)
            {
                level.SetName();
            }
        }

        #endregion
        
        public UpgradableInstanceConfigSO FindUpgradableInstanceConfigData(UpgradableBase target)
        {
            string[] guids = AssetDatabase.FindAssets($"t:UpgradableInstanceConfigSO {target.name}");
            
            if(guids.Length == 0) return null;

            var guid = guids.FirstOrDefault();
            
            if(guid == null) return null;
            
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<UpgradableInstanceConfigSO>(path);
        }

        public UpgradableInstanceConfigSO StoreConfigData(UpgradableInstanceConfigSO newConfig)
        {
            if (String.IsNullOrEmpty(upgradeName))
            {
                Debug.LogError($"Upgradable {name} has no name !");
                return null;
            }
            
            AssetDatabase.CreateAsset(newConfig, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newConfig;

            // newConfig.ReloadVVID();
            
            UpgradableInstances.AddUnique(newConfig);
            
            return newConfig;
        }

        [Button("Generate All Upgradable Configs")]
        protected void GenerateConfigs()
        {
            if (String.IsNullOrEmpty(upgradeName))
            {
                Debug.LogError($"Upgradable {name} has no 'upgradeName' !");
                return;
            }
            
            string currentScenePath = SceneManager.GetActiveScene().path;
            string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
            Debug.Log($"{guids.Length} to explore :");
            foreach (string guid in guids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                Debug.Log($"Processing scene: {scenePath}");
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                
                Debug.Log($"Searching in {scene.path}...");
                bool modified = false;
                UpgradableBase[] sceneUpgradables =
                    FindObjectsByType<UpgradableBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Debug.Log($"Found {sceneUpgradables.Length} upgradable objects.");
                foreach (UpgradableBase upgradable in sceneUpgradables)
                {
                    Debug.Log($"{upgradable.UpgradableSO.ID} == {ID}: {upgradable.UpgradableSO.ID == ID}");
                    if(!upgradable || !upgradable.UpgradableSO.ID.Equals(ID))
                        continue;

                    if (upgradable.InstanceConfigSO != null)
                    {
                        upgradable.SetInstanceName();
                        continue;
                    }
                        
                    Debug.Log($"Converting {upgradable.name}...");
                    
                    upgradable.GenerateInstanceConfigSO();
                    upgradable.gameObject.name = $"{upgradable.UpgradableSO.upgradeName}";
                    
                    EditorUtility.SetDirty(upgradable.gameObject);
                    EditorUtility.SetDirty(upgradable);
                    modified = true;
                }

                if (!modified) continue;
                
                EditorSceneManager.MarkSceneDirty(scene); // Mark scene dirty
                EditorSceneManager.SaveScene(scene);      // Save scene
                Debug.Log($"Saved changes to scene: {scenePath}");
            }
            
            EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);

            Save();
        }
#endif
    }
}