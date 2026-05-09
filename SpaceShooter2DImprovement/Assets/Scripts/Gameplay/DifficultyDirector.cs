using UnityEngine;

public class DifficultyDirector : MonoBehaviour
{
    [Header("Difficulty Thresholds")]
    public int firstWaveThreshold = 5;
    public int secondWaveThreshold = 10;

    [Header("Spawn Pressure")]
    public float firstWaveSpawnDelayMultiplier = 0.75f;
    public float secondWaveSpawnDelayMultiplier = 0.55f;

    [Header("Enemy Speed")]
    public float firstWaveEnemySpeedMultiplier = 1.2f;
    public float secondWaveEnemySpeedMultiplier = 1.45f;

    private EnemySpawner[] spawners;
    private float[] baseSpawnDelays;
    private int currentStage;

    private void Start()
    {
        spawners = FindObjectsOfType<EnemySpawner>();
        baseSpawnDelays = new float[spawners.Length];
        for (int i = 0; i < spawners.Length; i++)
        {
            baseSpawnDelays[i] = spawners[i].spawnDelay;
        }
        ApplyDifficulty(0);
    }

    private void Update()
    {
        int desiredStage = GetDesiredStage();
        if (desiredStage != currentStage)
        {
            ApplyDifficulty(desiredStage);
            if (FeedbackMessenger.instance != null)
            {
                FeedbackMessenger.instance.ShowMessage("Enemy wave intensified.");
            }
        }

        ApplyEnemySpeed();
    }

    private int GetDesiredStage()
    {
        if (GameManager.instance == null)
        {
            return 0;
        }
        if (GameManager.instance.EnemiesDefeated >= secondWaveThreshold)
        {
            return 2;
        }
        if (GameManager.instance.EnemiesDefeated >= firstWaveThreshold)
        {
            return 1;
        }
        return 0;
    }

    private void ApplyDifficulty(int stage)
    {
        currentStage = stage;
        float spawnMultiplier = GetSpawnDelayMultiplier();
        for (int i = 0; i < spawners.Length; i++)
        {
            if (spawners[i] != null)
            {
                spawners[i].spawnDelay = baseSpawnDelays[i] * spawnMultiplier;
            }
        }
    }

    private void ApplyEnemySpeed()
    {
        float speedMultiplier = GetEnemySpeedMultiplier();
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in enemies)
        {
            EnemyDifficultyRecord record = enemy.GetComponent<EnemyDifficultyRecord>();
            if (record == null)
            {
                record = enemy.gameObject.AddComponent<EnemyDifficultyRecord>();
                record.baseMoveSpeed = enemy.moveSpeed;
            }
            enemy.moveSpeed = record.baseMoveSpeed * speedMultiplier;
        }
    }

    private float GetSpawnDelayMultiplier()
    {
        if (currentStage >= 2)
        {
            return secondWaveSpawnDelayMultiplier;
        }
        if (currentStage == 1)
        {
            return firstWaveSpawnDelayMultiplier;
        }
        return 1f;
    }

    private float GetEnemySpeedMultiplier()
    {
        if (currentStage >= 2)
        {
            return secondWaveEnemySpeedMultiplier;
        }
        if (currentStage == 1)
        {
            return firstWaveEnemySpeedMultiplier;
        }
        return 1f;
    }
}
