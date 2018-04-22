using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class EditorBuildAllWindow : EditorWindow {

    string win_log = "[====>    ] -50% Complete";
    string mac_log = "[Mac] Uploading file";
    string lin_log = "[Linux] Downloading malware...";

    bool logChanged = false;

    public GUIStyle guiStyle;

    static EditorBuildAllWindow _inst;
    public static EditorBuildAllWindow Instance {
        get
        {
            if (_inst == null)
            {
                return (_inst = OpenWindow());
            }
            else return _inst;
        }
    }

    [MenuItem("Window/Open Build All")]
    public static EditorBuildAllWindow OpenWindow()
    {
        var a = GetWindow<EditorBuildAllWindow>();
        a.guiStyle = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).textArea;

        Texture2D t = new Texture2D(2, 2);
        t.SetPixels(new Color[] {Color.black, Color.black, Color.black, Color.black});
        t.filterMode = FilterMode.Point;
        t.wrapMode = TextureWrapMode.Repeat;
        t.Apply();

        a.guiStyle.normal.background = t;
        a.guiStyle.normal.textColor = Color.white;

        a.guiStyle.focused.background = t;
        a.guiStyle.focused.textColor = Color.white;

        a.guiStyle.active = a.guiStyle.normal;
        a.guiStyle.onHover = a.guiStyle.normal;

        a.guiStyle.font = Font.CreateDynamicFontFromOSFont("Consolas", 12);
        a.guiStyle.fontSize = 12;

        a.minSize = new Vector2(3 * 12 * 50 + 10, 12 * 25);
        a.Show();

        return a;
    }

    private void Update()
    {
        if (logChanged) Repaint();
        logChanged = false;
    }

    public void LogLine(LogPlatform platform, string log)
    {
        switch (platform)
        {
            case LogPlatform.Windows:
                win_log += log + "\n";
                break;
            case LogPlatform.Mac:
                mac_log += log + "\n";
                break;
            case LogPlatform.Linux:
                lin_log += log + "\n";
                break;
        }
        logChanged = true;
    }

    public void ClearLog(LogPlatform platform, string log)
    {
        return;
        switch (platform)
        {
            case LogPlatform.Windows:
                win_log = "";
                break;
            case LogPlatform.Mac:
                mac_log = "";
                break;
            case LogPlatform.Linux:
                lin_log = "";
                break;
        }
        logChanged = true;
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        //Windows
        EditorGUILayout.BeginVertical();
        EditorGUILayout.SelectableLabel(win_log, guiStyle, GUILayout.MinHeight(12 * 25), GUILayout.MinWidth(12 * 50));
        EditorGUILayout.EndVertical();

        //Mac
        EditorGUILayout.BeginVertical();
        EditorGUILayout.SelectableLabel(mac_log, guiStyle, GUILayout.MinHeight(12 * 25), GUILayout.MinWidth(12 * 50));
        EditorGUILayout.EndVertical();

        //Linux
        EditorGUILayout.BeginVertical();
        EditorGUILayout.SelectableLabel(lin_log, guiStyle, GUILayout.MinHeight(12 * 25), GUILayout.MinWidth(12 * 50));
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }
}
public enum LogPlatform { Windows, Mac, Linux }
