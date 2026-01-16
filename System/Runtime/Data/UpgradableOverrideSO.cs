using System;
using System.Collections.Generic;
using UnityEngine;

namespace VV.Upgradable
{
    [CreateAssetMenu(menuName = "VV/UpgradableObjects/UpgradableOverride", fileName = "UpgradableOverride")]
    public class UpgradableOverrideSO : ScriptableObject
    {
        [SerializeField] private string overrideName;
        [SerializeField] private int maxLevelOverride = -1; // -1 means no override
        [SerializeField] private List<UpgradeConditionSO> additionalGlobalConditions = new();
        [SerializeField] private List<LevelDataOverride> levelOverrides = new();
        
        [Serializable]
        public class LevelDataOverride
        {
            [SerializeField] private int level;
            [SerializeField] private int scoreOverride = -1; // -1 means no override
            [SerializeField] private List<UpgradeConditionSO> additionalConditions = new();
            [SerializeField] private List<UpgradeConditionSO> replacementConditions = new();
            
            public int Level => level;
            public int ScoreOverride => scoreOverride;
            public List<UpgradeConditionSO> AdditionalConditions => additionalConditions;
            public List<UpgradeConditionSO> ReplacementConditions => replacementConditions;
        }
        
        public string OverrideName => overrideName;
        public int MaxLevelOverride => maxLevelOverride;
        public List<UpgradeConditionSO> AdditionalGlobalConditions => additionalGlobalConditions;
        public List<LevelDataOverride> LevelOverrides => levelOverrides;
    }
}