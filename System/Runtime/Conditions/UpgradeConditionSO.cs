using UnityEngine;

namespace VV.Upgradable
{
    public abstract class UpgradeConditionSO : ScriptableObject, IUpgradeCondition
    {
        public abstract bool CanUpgrade();
        public abstract void Init();
    }
}