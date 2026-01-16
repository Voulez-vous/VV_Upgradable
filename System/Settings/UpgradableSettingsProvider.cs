using System.Collections.Generic;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VV.Upgradable.Settings
{
#if UNITY_EDITOR
    public class UpgradableSettingsProvider : SettingsProvider
    {
        private SerializedObject m_CustomSettings;
        
        static string customSettingsPath = $"Assets/Resources/{UpgradableSettings.SettingsName}.asset";
        
        public UpgradableSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
        }
        
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            // This function is called when the user clicks on the MyCustom element in the Settings window.
            m_CustomSettings = UpgradableSettings.GetSerializedSettings();
        }

        public override void OnGUI(string searchContext)
        {
            // Use IMGUI to display UI:
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("activeUpgradables"));
            m_CustomSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        // Register the SettingsProvider
        [SettingsProvider]
        public static SettingsProvider CreateCustomSettingsProvider()
        {
            // Settings Asset doesn't exist yet; no need to display anything in the Settings window.
            return new UpgradableSettingsProvider("Project/VV/Upgradables", SettingsScope.Project);
        }
    }
#endif
}