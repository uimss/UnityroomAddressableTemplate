using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[FilePath("ProjectSettings/UnityroomBuilderSettings.asset", FilePathAttribute.Location.ProjectFolder)]
public class UnityroomBuilderSettings : ScriptableSingleton<UnityroomBuilderSettings>
{
    public string StreamingAssetsUrl;

    public string GetParsedStreamingAssetsUrl()
    {
        return StreamingAssetsUrl.Replace("{version}", Application.version);
    }

    public void Save()
    {
        Save(true);
    }

    public static SerializedObject GetSerializedObject()
    {
        instance.hideFlags = HideFlags.HideAndDontSave & ~HideFlags.NotEditable;
        return new SerializedObject(instance);
    }
}

class UnityroomBuilderSettingsProvider : SettingsProvider
{
    SerializedObject m_settings;
    Editor _editor;

    public UnityroomBuilderSettingsProvider(
        string path,
        SettingsScope scopes,
        IEnumerable<string> keywords = null) : base(path, scopes, keywords)
    {
    }

    public override void OnActivate(string searchContext, VisualElement rootElement)
    {
        m_settings = UnityroomBuilderSettings.GetSerializedObject();
    }

    public override void OnGUI(string searchContext)
    {
        m_settings.Update();
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(m_settings.FindProperty("StreamingAssetsUrl"));
        if (EditorGUI.EndChangeCheck())
        {
            m_settings.ApplyModifiedProperties();
            UnityroomBuilderSettings.instance.Save();
        }
    }

    [SettingsProvider]
    public static SettingsProvider CreateProvider()
    {
        var path = "Project/Unityroom Builder";
        var scopes = SettingsScope.Project;
        var settings = UnityroomBuilderSettings.GetSerializedObject();
        var keywords = GetSearchKeywordsFromSerializedObject(settings);

        return new UnityroomBuilderSettingsProvider(path, scopes, keywords);
    }
}
