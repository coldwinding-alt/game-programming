using System.Collections.Generic;
using UnityEngine;

public class PlayerPowerUpController : MonoBehaviour
{
    [Header("References")]
    public Controller movementController;
    public Health playerHealth;
    public ShootingController[] shootingControllers;

    [Header("Tuning")]
    public float rapidFireMultiplier = 0.35f;
    public float speedBoostMultiplier = 1.45f;

    private float rapidFireTimer;
    private float shieldTimer;
    private float speedBoostTimer;
    private float baseMoveSpeed;
    private readonly Dictionary<ShootingController, float> baseFireRates = new Dictionary<ShootingController, float>();

    public string ActivePowerUpLabel
    {
        get
        {
            List<string> active = new List<string>();
            if (rapidFireTimer > 0f)
            {
                active.Add("Rapid Fire " + Mathf.CeilToInt(rapidFireTimer) + "s");
            }
            if (shieldTimer > 0f)
            {
                active.Add("Shield " + Mathf.CeilToInt(shieldTimer) + "s");
            }
            if (speedBoostTimer > 0f)
            {
                active.Add("Speed " + Mathf.CeilToInt(speedBoostTimer) + "s");
            }
            if (active.Count == 0)
            {
                return "Power-up: none";
            }
            return "Power-up: " + string.Join(" | ", active);
        }
    }

    private void Awake()
    {
        if (movementController == null)
        {
            movementController = GetComponent<Controller>();
        }
        if (playerHealth == null)
        {
            playerHealth = GetComponent<Health>();
        }
        if (shootingControllers == null || shootingControllers.Length == 0)
        {
            shootingControllers = GetComponentsInChildren<ShootingController>();
        }

        if (movementController != null)
        {
            baseMoveSpeed = movementController.moveSpeed;
        }
        foreach (ShootingController shootingController in shootingControllers)
        {
            if (shootingController != null && !baseFireRates.ContainsKey(shootingController))
            {
                baseFireRates.Add(shootingController, shootingController.fireRate);
            }
        }
    }

    private void Update()
    {
        TickTimers();
        ApplyTimedValues();
    }

    public void ApplyPowerUp(PowerUpType powerUpType, float duration, int healAmount)
    {
        switch (powerUpType)
        {
            case PowerUpType.RapidFire:
                rapidFireTimer = Mathf.Max(rapidFireTimer, duration);
                ShowPickupMessage("Rapid fire online.");
                break;
            case PowerUpType.Shield:
                shieldTimer = Mathf.Max(shieldTimer, duration);
                ShowPickupMessage("Shield active.");
                break;
            case PowerUpType.Heal:
                if (playerHealth != null)
                {
                    playerHealth.ReceiveHealing(healAmount);
                }
                ShowPickupMessage("Repair kit collected.");
                break;
            case PowerUpType.SpeedBoost:
                speedBoostTimer = Mathf.Max(speedBoostTimer, duration);
                ShowPickupMessage("Engines boosted.");
                break;
        }
        GameManager.UpdateUIElements();
        ApplyTimedValues();
    }

    private void TickTimers()
    {
        rapidFireTimer = Mathf.Max(0f, rapidFireTimer - Time.deltaTime);
        shieldTimer = Mathf.Max(0f, shieldTimer - Time.deltaTime);
        speedBoostTimer = Mathf.Max(0f, speedBoostTimer - Time.deltaTime);
    }

    private void ApplyTimedValues()
    {
        if (movementController != null)
        {
            movementController.moveSpeed = speedBoostTimer > 0f ? baseMoveSpeed * speedBoostMultiplier : baseMoveSpeed;
        }

        foreach (KeyValuePair<ShootingController, float> entry in baseFireRates)
        {
            if (entry.Key != null)
            {
                entry.Key.fireRate = rapidFireTimer > 0f ? entry.Value * rapidFireMultiplier : entry.Value;
            }
        }

        if (playerHealth != null)
        {
            playerHealth.isAlwaysInvincible = shieldTimer > 0f;
        }
    }

    private void ShowPickupMessage(string message)
    {
        if (FeedbackMessenger.instance != null)
        {
            FeedbackMessenger.instance.ShowMessage(message);
        }
    }
}
