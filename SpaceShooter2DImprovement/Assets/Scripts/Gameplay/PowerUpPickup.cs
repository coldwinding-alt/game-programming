using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    [Header("Power-up")]
    public PowerUpType powerUpType = PowerUpType.RapidFire;
    public float duration = 8f;
    public int healAmount = 1;
    public int scoreBonus = 5;

    [Header("Feedback")]
    public AudioClip pickupSound;
    public GameObject pickupEffect;
    public Color tint = Color.white;

    private void Start()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = tint;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerPowerUpController playerPowerUpController = collision.GetComponent<PlayerPowerUpController>();
        if (playerPowerUpController == null)
        {
            playerPowerUpController = collision.GetComponentInParent<PlayerPowerUpController>();
        }

        if (playerPowerUpController == null)
        {
            return;
        }

        playerPowerUpController.ApplyPowerUp(powerUpType, duration, healAmount);
        if (scoreBonus > 0 && GameManager.instance != null && !GameManager.instance.gameIsOver)
        {
            GameManager.AddScore(scoreBonus);
        }
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, transform.rotation, null);
        }
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, 0.8f);
        }
        Destroy(gameObject);
    }
}
