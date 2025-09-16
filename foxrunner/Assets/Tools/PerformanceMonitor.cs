using UnityEditor;
using UnityEngine;

public class PerformanceMonitor : MonoBehaviour
{
    [Header("=== DISPLAY SETTINGS ===")]
    [SerializeField] private bool showFPS = true;
    [SerializeField] private bool showMemory = true;
    [SerializeField] private bool showDrawCalls = true;

    private float deltaTime;
    private float fps;
    private float memoryUsage;

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        fps = 1.0f / deltaTime;
        memoryUsage = System.GC.GetTotalMemory(false) / 1048576f; // MB
    }

    void OnGUI()
    {
        int w = Screen.width, h = Screen.height;
        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(w - 200, 0, 200, 100);
        style.alignment = TextAnchor.UpperRight;
        style.fontSize = h / 40;
        style.normal.textColor = GetFPSColor();

        string text = "";
        if (showFPS) text += $"FPS: {fps:0.}\n";
        if (showMemory) text += $"Memory: {memoryUsage:0.0} MB\n";
        if (showDrawCalls) text += $"Draw Calls: {UnityStats.drawCalls}\n";

        GUI.Label(rect, text, style);
    }

    Color GetFPSColor()
    {
        if (fps >= 55) return Color.green;
        if (fps >= 30) return Color.yellow;
        return Color.red;
    }
}