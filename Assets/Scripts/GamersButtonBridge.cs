using Gamers.Client.Samples;
using TMPro;
using UnityEngine;

public class GamersButtonBridge : MonoBehaviour
{
    public ReferenceIntegration gamers;

    [Header("User Inputs")]
    public TMP_InputField emailInput;
    public TMP_InputField codeInput;
    public TMP_InputField tournamentIdInput;
    public TMP_InputField eventIdInput;

    void Start()
    {
        if (gamers == null)
            gamers = GetComponent<ReferenceIntegration>();
    }

    public void RequestAuth()
    {
        gamers.OnRequestAuth(emailInput.text.Trim());
    }

    public void SubmitCode()
    {
        gamers.OnSubmitCode(
            emailInput.text.Trim(),
            codeInput.text.Trim()
        );
    }

    public void JoinTournament()
    {
        gamers.OnJoinTournament(tournamentIdInput.text.Trim());
    }

    public void JoinEvent()
    {
        gamers.OnJoinEvent(eventIdInput.text.Trim());
    }

    public void ShowLeaderboard()
    {
        gamers.OnShowLeaderboard(tournamentIdInput.text.Trim());
    }
}