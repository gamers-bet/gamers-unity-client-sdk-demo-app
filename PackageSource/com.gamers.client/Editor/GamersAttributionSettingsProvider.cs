using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;

namespace Gamers.Client.Editor
{
    /// <summary>
    /// Project Settings page (Project Settings → Gamers) that sends Unity's Verified Solutions
    /// attribution event once per project.
    /// </summary>
    /// <remarks>
    /// The developer ID entered here comes from the Gamers developer portal and is used solely for
    /// Unity's Verified Solutions attribution. It is stored in this project's per-user editor
    /// settings (<see cref="EditorUserSettings"/>), is never read by runtime code, and is never
    /// included in player builds — this assembly is Editor-only. Sending respects the Unity Editor
    /// analytics preference: if the user has disabled Editor analytics, nothing is sent.
    /// </remarks>
    public static class GamersAttributionSettingsProvider
    {
        // TODO: confirm the exact partner name string with the Unity Verified Solutions POC.
        private const string PartnerName = "Gamers";
        private const string ActionName = "Configure";
        private const string DeveloperIdConfigKey = "com.gamers.client.developer-id";
        private const string AttributionSentConfigKey = "com.gamers.client.vs-attribution-sent";

        /// <summary>Creates the "Project Settings → Gamers" settings page.</summary>
        /// <returns>The Editor settings provider discovered by Unity.</returns>
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/Gamers", SettingsScope.Project)
            {
                label = "Gamers",
                keywords = new HashSet<string> { "Gamers", "Attribution", "Verified", "Solutions", "Developer" },
                guiHandler = _ => DrawGui()
            };
        }

        private static void DrawGui()
        {
            EditorGUILayout.LabelField("Verified Solutions Attribution", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Enter your Gamers developer ID (from the Gamers developer portal). It is used once to " +
                "send Unity's Verified Solutions attribution event, is stored only in this project's " +
                "editor user settings, and never ships in player builds.",
                MessageType.Info);

            var currentId = EditorUserSettings.GetConfigValue(DeveloperIdConfigKey) ?? string.Empty;
            var newId = EditorGUILayout.DelayedTextField("Developer ID", currentId);
            if (newId != currentId)
            {
                EditorUserSettings.SetConfigValue(DeveloperIdConfigKey, newId);
                currentId = newId;
                if (!string.IsNullOrWhiteSpace(newId))
                    TrySendAttributionEvent(newId);
            }

            if (HasSentAttribution())
            {
                EditorGUILayout.LabelField("Attribution event already sent for this project.", EditorStyles.miniLabel);
                return;
            }

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(currentId)))
            {
                if (GUILayout.Button("Send Attribution Event", GUILayout.Width(180f)))
                    TrySendAttributionEvent(currentId);
            }
        }

        private static bool HasSentAttribution()
        {
            return EditorUserSettings.GetConfigValue(AttributionSentConfigKey) == "true";
        }

        private static void TrySendAttributionEvent(string developerId)
        {
            if (HasSentAttribution())
                return;

            var result = VSAttribution.SendAttributionEvent(ActionName, PartnerName, developerId);
            switch (result)
            {
                case AnalyticsResult.Ok:
                    EditorUserSettings.SetConfigValue(AttributionSentConfigKey, "true");
                    Debug.Log("[Gamers] Verified Solutions attribution event sent.");
                    break;
                case AnalyticsResult.AnalyticsDisabled:
                    // Editor analytics are disabled in Unity preferences; respect that silently.
                    break;
                default:
                    Debug.LogWarning($"[Gamers] Verified Solutions attribution event was not sent: {result}");
                    break;
            }
        }
    }
}
