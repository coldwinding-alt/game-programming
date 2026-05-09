using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject pickupPrefab;
    public Sprite[] pickupSprites;
    public AudioClip pickupSound;
    public GameObject pickupEffect;

    [Header("Spawn Area")]
    public Vector2 center = Vector2.zero;
    public Vector2 size = new Vector2(16f, 8f);
    public Vector2 spawnDelayRange = new Vector2(12f, 16f);

    private float nextSpawnTime;

    private readonly Color[] powerUpColors =
    {
        new Color(0.2f, 0.85f, 1f, 1f),
        new Color(0.4f, 1f, 0.45f, 1f),
        new Color(1f, 0.35f, 0.45f, 1f),
        new Color(1f, 0.9f, 0.25f, 1f)
    };

    private void Start()
    {
        ScheduleNextSpawn(2f);
    }

    private void Update()
    {
        if (GameManager.instance != null && GameManager.instance.gameIsOver)
        {
            return;
        }

        if (Time.time >= nextSpawnTime)
        {
            SpawnPickup();
            ScheduleNextSpawn(0f);
        }
    }

    private void SpawnPickup()
    {
        if (pickupPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = new Vector3(
            center.x + Random.Range(-size.x * 0.5f, size.x * 0.5f),
            center.y + Random.Range(-size.y * 0.5f, size.y * 0.5f),
            0f);
        GameObject pickupObject = Instantiate(pickupPrefab, spawnPosition, Quaternion.identity, transform);
        PowerUpPickup pickup = pickupObject.GetComponent<PowerUpPickup>();
        SpriteRenderer spriteRenderer = pickupObject.GetComponent<SpriteRenderer>();
        PowerUpType type = (PowerUpType)Random.Range(0, System.Enum.GetValues(typeof(PowerUpType)).Length);

        if (pickup != null)
        {
            pickup.powerUpType = type;
            pickup.pickupSound = pickupSound;
            pickup.pickupEffect = pickupEffect;
            pickup.tint = powerUpColors[(int)type];
        }
        if (spriteRenderer != null)
        {
            if (pickupSprites != null && pickupSprites.Length > 0)
            {
                spriteRenderer.sprite = pickupSprites[Random.Range(0, pickupSprites.Length)];
            }
            spriteRenderer.color = powerUpColors[(int)type];
        }
    }

    private void ScheduleNextSpawn(float extraDelay)
    {
        nextSpawnTime = Time.time + extraDelay + Random.Range(spawnDelayRange.x, spawnDelayRange.y);
    }
}
