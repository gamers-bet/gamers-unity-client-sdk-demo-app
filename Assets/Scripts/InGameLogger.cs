using TMPro;
using UnityEngine;

public class InGameLogger : MonoBehaviour
{
    [SerializeField] private TMP_Text outputText;
    [SerializeField] private int maxCharacters = 20000;

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        string color = type switch
        {
            LogType.Error => "#FF5555",
            LogType.Assert => "#FF5555",
            LogType.Exception => "#FF5555",
            LogType.Warning => "#FFCC00",
            _ => "#FFFFFF"
        };

        string formattedMessage = $"<color={color}>[{type}] {logString}</color>\n";
        outputText.text += formattedMessage;

        // Truncate old output if text gets too long
        if (outputText.text.Length > maxCharacters)
        {
            outputText.text = outputText.text[^maxCharacters..];
        }
    }
}