using UnityEngine;
using System.Collections.Generic;

public class DebugConsole : MonoBehaviour
{
    [Header("=== CONFIGURATION ===")]
    [SerializeField] private bool showConsole = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;
    [SerializeField] private int maxLines = 50;

    private List<string> debugLog = new List<string>();
    private bool isVisible = false;
    private Vector2 scrollPosition;

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
        }
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        string formatted = $"[{System.DateTime.Now:HH:mm:ss}] [{type}] {logString}";
        debugLog.Add(formatted);

        if (debugLog.Count > maxLines)
        {
            debugLog.RemoveAt(0);
        }
    }

    void OnGUI()
    {
        if (!showConsole || !isVisible) return;

        GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height / 3));
        GUILayout.BeginVertical("box");

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        foreach (string log in debugLog)
        {
            GUILayout.Label(log);
        }
        GUILayout.EndScrollView();

        if (GUILayout.Button("Clear"))
        {
            debugLog.Clear();
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}