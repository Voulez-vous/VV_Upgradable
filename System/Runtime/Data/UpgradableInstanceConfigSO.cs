using System;
using UnityEngine;
using VV.SO;
using VV.Utility;

namespace VV.Upgradable
{
    [CreateAssetMenu(menuName = "VV/UpgradableObjects/UpgradableInstanceConfig", fileName = "UpgradableInstanceConfig")]
    public class UpgradableInstanceConfigSO : SerializableScriptableObject
    {
        [SerializeField] private string instanceName;
        [SerializeField] [ReadOnly] private string sceneName;
        
        [SerializeField] private UpgradableOverrideSO overrides;
        
        public string ID => VVGuid.ToString();

        public string InstanceName => instanceName;
        
        public string SceneName
        {
            get => sceneName;
            set => sceneName = value;
        }

        public UpgradableOverrideSO Overrides => overrides;

        private bool Equals(UpgradableInstanceConfigSO other)
        {
            return string.Equals(ID, other.ID);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((UpgradableInstanceConfigSO)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), ID);
        }

#if UNITY_EDITOR
        public void SetName(string newName)
        {
            instanceName = newName;
            Save();
        }

        public void SetSceneName(string newName)
        {
            sceneName = newName;
            Save();
        }
#endif
    }
}