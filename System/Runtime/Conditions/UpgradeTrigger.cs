using UnityEngine;

namespace VV.Upgradable
{
    public class UpgradeTrigger : MonoBehaviour
    {
        [SerializeField] UpgradableSO upgradableSO;
        [SerializeField] UpgradableInstanceConfigSO instanceConfigSO;

        /// <summary>
        /// Allows designers to trigger upgrade of any upgradable's instance at any time.
        /// You don't even need to be in the same upgradable's scene.
        /// </summary>
        public void Trigger()
        {
            if(!upgradableSO) return;
            string id = instanceConfigSO ? instanceConfigSO.ID : upgradableSO.ID;
            UpgradeManager.UpgradableStateManagers.TryGetValue(id, out UpgradableStateManager upgradableState);
            upgradableState?.LevelUp();
        }
    }
}