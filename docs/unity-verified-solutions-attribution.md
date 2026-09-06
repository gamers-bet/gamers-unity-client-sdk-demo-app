# Verified Solutions Attribution — Implementation Summary

We have integrated VS Attribution into `com.gamers.client` as a **static script dependency**, following the official [Unity VS Attribution integration guide](https://github.com/Unity-Technologies/com.unity.vs-attribution).

## Files added

- `Editor/VSAttribution.cs` — copy of Unity's `VSAttribution.cs` script. The default `VS` namespace has been changed to `Gamers.Client.Editor` to avoid namespace clashes with other Verified Solutions packages.
- `Editor/GamersAttributionSettingsProvider.cs` — Unity `SettingsProvider` that adds a **Project Settings → Gamers** page for collecting the customer identifier.
- `Editor/GamersClient.Editor.asmdef` — Editor-only assembly definition (`includePlatforms: ["Editor"]`), so the attribution code is excluded from every player build.

## How it works

1. A developer installs `com.gamers.client` and opens **Project Settings → Gamers**.
2. They enter their **Gamers developer/studio ID** (a unique, non-secret identifier from the Gamers developer portal).
3. On first valid input, the SDK calls:

   ```csharp
   VSAttribution.SendAttributionEvent("Configure", "Gamers", developerId);
   ```

4. A per-project flag in `EditorUserSettings` ensures the event is sent **only once per project**.
5. If the user has disabled Unity Editor Analytics in Preferences, the call returns `AnalyticsDisabled` and no event is transmitted.

## customerUid mapping

The `customerUid` is the **Gamers developer ID**. It maps to a unique customer record in our backend (the studio/team entity in the Gamers developer portal), satisfying the VS Attribution requirement that the identifier translate to a specific customer on our end. No personal user data is captured; only `actionName`, `partnerName`, and `customerUid` are sent.

## How to verify

1. Import `com.gamers.client` into a Unity project
2. Open **Edit → Project Settings → Gamers**.
3. Enter a Gamers developer ID and press Enter or click **Send Attribution Event**.
4. The page updates to show **"Attribution event already sent for this project."**, confirming the event was fired.

## Security and runtime impact

- The entire attribution implementation lives in an **Editor-only assembly** and is stripped from iOS, Android, and all other player builds.
- The runtime SDK remains **credential-free**: no API key, JWT, or attribution data is accessible at runtime.

## Privacy

Our privacy policy allows sharing this unique customer identifier with Unity for the purposes of the Verified Solutions attribution integration, as described in the VS Attribution Legal Disclaimer.
