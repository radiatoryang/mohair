using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
#endif
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#endif

namespace Mohair
{

    /// <summary>
    /// Mohair project settings shown in the "Project Settings" window
    /// </summary>
    public class MohairSettingsProvider : SettingsProvider
    {
        /// <summary>container class for Mohair project settings so we can easily write to Json</summary>
        [System.Serializable]
        public class MohairSettings {
            public string markdownFilePath = "Assets/Dialogue/YarnDocumentation.md";
            public string markdownFilePathFull { get { return MohairSettingsProvider.projectFolder + markdownFilePath;} }

            public string htmlFilePath = "Assets/Dialogue/YarnDocumentationWeb.html";
            public string htmlFilePathFull { get { return MohairSettingsProvider.projectFolder + htmlFilePath;} }

            public string markdownTemplateFilePath = "Packages/com.radiatoryang.mohair/Editor/MohairTemplate.md";
            public string markdownTemplateFilePathFull { get { return MohairSettingsProvider.projectFolder + markdownTemplateFilePath;} }

            public string htmlTemplateFilePath = "Packages/com.radiatoryang.mohair/Editor/MohairTemplateWeb.html";
            public string htmlTemplateFilePathFull { get { return MohairSettingsProvider.projectFolder + htmlTemplateFilePath;} }

            public System.Text.Encoding encoding { get { return System.Text.Encoding.UTF8; } }
            public string[] cSharpIgnore { get { return cSharpIgnoreString.Split(new string[] {",", " "}, System.StringSplitOptions.RemoveEmptyEntries ); } }
            public string cSharpIgnoreString = "editor, package, test";

            public void LoadSettings() {
                if ( File.Exists( settingsFilePath) ) {
                    var json = File.ReadAllText( settingsFilePath );
                    EditorJsonUtility.FromJsonOverwrite(json, _settings);
                } 
            }

            public void SaveSettings() {
                string jsonData = EditorJsonUtility.ToJson(Settings, true);
                File.WriteAllText( settingsFilePath, jsonData );
            }
        }

        public static string projectFolder { get { return Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);} }
        static MohairSettings _settings;
        public static MohairSettings Settings {
            get {
                if ( _settings == null ) {
                    _settings = new MohairSettings();
                    _settings.LoadSettings();
                }
                return _settings;
            }   
            set {
                _settings = value;
            }
        }
        static string settingsFilePath { get { return projectFolder + "ProjectSettings/MohairSettings.json"; } }

        public MohairSettingsProvider(string path, SettingsScope scope = SettingsScope.Project) : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            if ( File.Exists( settingsFilePath) ) {
                var json = File.ReadAllText( settingsFilePath );
                EditorJsonUtility.FromJsonOverwrite(json, Settings);
            }
        }

        public override void OnDeactivate()
        {
            Settings.SaveSettings();
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUI.indentLevel = 1;
            EditorGUIUtility.labelWidth = 256;
            GUILayout.BeginVertical();
            EditorGUI.BeginChangeCheck();

            GUILayout.Space(5);
            GUILayout.Label("File paths", EditorStyles.boldLabel);
            GUILayout.Label($"All file paths are relative to your project folder.\n({projectFolder})");

            Settings.markdownFilePath = EditorGUILayout.DelayedTextField(
                new GUIContent("Save Markdown documentation to...", "default: 'Assets/Dialogue/YarnDocumentation.md' "), 
                Settings.markdownFilePath
            );
            Settings.htmlFilePath = EditorGUILayout.DelayedTextField(
                new GUIContent("Save HTML documentation to...", "default: 'Assets/Dialogue/YarnDocumentation.html' "), 
                Settings.htmlFilePath
            );

            Settings.markdownTemplateFilePath = EditorGUILayout.DelayedTextField(
                new GUIContent("Load Markdown template from... ", "default: 'Packages/com.radiatoryang.mohair/Editor/MohairTemplate.md' "), 
                Settings.markdownTemplateFilePath
            );
            Settings.htmlTemplateFilePath = EditorGUILayout.DelayedTextField(
                new GUIContent("Load HTML template from... ", "default: 'Packages/com.radiatoryang.mohair/Editor/MohairTemplateWeb.html' "), 
                Settings.htmlTemplateFilePath
            );

            GUILayout.Space(5);
            GUILayout.Label("Other Settings", EditorStyles.boldLabel);

            Settings.cSharpIgnoreString = EditorGUILayout.DelayedTextField(
                new GUIContent("Ignore C# files with... ", "when scanning C# file paths, Mohair will ignores .cs file paths that contain any of these case-insensitive comma-separated words... default: 'editor, packages, test' "), 
                Settings.cSharpIgnoreString
            );

            // oops, encoding isn't an enum
            // Settings.encoding = (System.Text.Encoding)EditorGUILayout.EnumPopup( (System.Enum)Settings.encoding );

            if ( EditorGUI.EndChangeCheck() ) {
                Settings.SaveSettings();
            }
            GUILayout.EndVertical();
            EditorGUIUtility.labelWidth = 0;
        }


        [SettingsProvider]
        public static SettingsProvider CreatePreferencesSettingsProvider()
        {
            var provider = new MohairSettingsProvider("Project/Mohair", SettingsScope.Project);

            provider.keywords = new HashSet<string>(new[] { "Yarn", "Documentation", "markdown", "html", "file", "path", "template" });

            return provider;
        }
    }

}
