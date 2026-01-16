using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VV.Upgradable.Settings
{
    public class UpgradableSettings : ScriptableObject
    {
        public static string SettingsName => "UpgradableSettings";
        public static string SettingsPath => $"Assets/Resources/VV/Upgradables/";
        public static string SettingsFullPath => $"{SettingsPath}/{SettingsName}.asset";
        
        [SerializeField] public List<UpgradableSO> activeUpgradables = new();
        
#if UNITY_EDITOR

        private void OnValidate()
        {
            GenerateEnum();
        }

        public void GenerateEnum()
        {
            string fileName = "UpgradeType.cs";
            string namespaceEnum = "Upgradable";
            string assetPath = Path.GetDirectoryName("Packages/com.vv.upgradable/System/");
            string path = string.Concat(assetPath, Path.DirectorySeparatorChar, fileName);
            FileStream fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write);
            fs.SetLength(0);
            StreamWriter sr = new StreamWriter(fs);
            sr.Write("using System;\n" +
                     $"namespace VV.{namespaceEnum}\n" +
                     "{\n" +
                     "    [Serializable]\n" +
                     $"    public enum {fileName.Replace(".cs", "")}\n" +
                     "    {\n");
            foreach (UpgradableSO upgradableSo in 
                     activeUpgradables
                         .Where(upgradableSo => 
                             upgradableSo != null && !string.IsNullOrEmpty(upgradableSo.UpgradeName)))
            {
                sr.WriteLine($"        {upgradableSo.UpgradeName.Replace(" ", "")},");
            }
            sr.WriteLine("        None,");
            sr.Write("    }\n" +
                     "}");
            sr.Close();
            
            AssetDatabase.Refresh();
        }
        
        public void FindUpgradables()
        {
            activeUpgradables.Clear();

            string[] guids = AssetDatabase.FindAssets("t:UpgradableSO");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UpgradableSO upgradable = AssetDatabase.LoadAssetAtPath<UpgradableSO>(path);

                if (upgradable != null && !activeUpgradables.Contains(upgradable))
                {
                    activeUpgradables.Add(upgradable);
                }
            }

            // Save the updated asset
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[UpgradableSettings] Found and assigned {activeUpgradables.Count} upgradables.");
        }
        
        [ContextMenu("Find All Collections")]
        private void FindAllCollectionsFromContextMenu()
        {
            FindUpgradables();
        }

        private static UpgradableSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<UpgradableSettings>(SettingsFullPath);
            
            if (settings != null) return settings;
            
            if(!Directory.Exists(SettingsPath))
                Directory.CreateDirectory(SettingsPath);
            
            settings = CreateInstance<UpgradableSettings>();
            settings.FindUpgradables();
            AssetDatabase.CreateAsset(settings, SettingsFullPath);
            AssetDatabase.SaveAssets();

            return settings;
        }

        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
#endif
    }
}