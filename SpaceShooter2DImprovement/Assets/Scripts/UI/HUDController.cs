using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [Header("Text")]
    public Text scoreText;
    public Text livesText;
    public Text objectiveText;
    public Text powerUpText;

    [Header("References")]
    public Health playerHealth;
    public PlayerPowerUpController playerPowerUpController;

    private void Start()
    {
        FindPlayerReferences();
        UpdateHUD();
    }

    private void Update()
    {
        UpdateHUD();
    }

    public void UpdateHUD()
    {
        FindPlayerReferences();

        if (scoreText != null)
        {
            scoreText.text = "Score: " + GameManager.score;
        }
        if (livesText != null)
        {
            livesText.text = GetLivesText();
        }
        if (objectiveText != null)
        {
            objectiveText.text = GetObjectiveText();
        }
        if (powerUpText != null)
        {
            powerUpText.text = playerPowerUpController != null ? playerPowerUpController.ActivePowerUpLabel : "Power-up: none";
        }
    }

    private void FindPlayerReferences()
    {
        if (GameManager.instance == null || GameManager.instance.player == null)
        {
            return;
        }

        if (playerHealth == null)
        {
            playerHealth = GameManager.instance.player.GetComponent<Health>();
        }
        if (playerPowerUpController == null)
        {
            playerPowerUpController = GameManager.instance.player.GetComponent<PlayerPowerUpController>();
        }
    }

    private string GetLivesText()
    {
        if (playerHealth == null)
        {
            return "Lives: --";
        }
        if (playerHealth.useLives)
        {
            return "Lives: " + playerHealth.currentLives + "  Health: " + playerHealth.currentHealth + "/" + playerHealth.maximumHealth;
        }
        return "Health: " + playerHealth.currentHealth + "/" + playerHealth.maximumHealth;
    }

    private string GetObjectiveText()
    {
        if (GameManager.instance == null)
        {
            return "Defeat 15 enemies. Collect power-ups. Survive.";
        }
        return "Objective: Defeat " + GameManager.instance.EnemiesToDefeat + " enemies  " +
            GameManager.instance.EnemiesDefeated + "/" + GameManager.instance.EnemiesToDefeat;
    }
}
