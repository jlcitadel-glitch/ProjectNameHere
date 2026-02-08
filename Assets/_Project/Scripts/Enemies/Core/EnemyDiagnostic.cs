using UnityEngine;
using ProjectName.UI;

/// <summary>
/// Runtime diagnostic for the enemy pipeline.
/// Add to any GameObject in the scene. Runs validation on Start and prints
/// a color-coded report to the Console.
/// Remove from the scene when no longer needed.
/// </summary>
public class EnemyDiagnostic : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool runOnStart = true;
    [SerializeField] private bool destroyAfterRun = false;

    [Header("Live Monitoring")]
    [Tooltip("Continuously monitor spawned enemies and log state/velocity")]
    [SerializeField] private bool liveMonitor = true;
    [SerializeField] private float monitorInterval = 3f;

    private float monitorTimer;
    private int lastEnemyCount;

    private void Start()
    {
        if (runOnStart)
        {
            RunDiagnostics();

            if (destroyAfterRun)
                Destroy(this);
        }
    }

    private void Update()
    {
        // Always monitor — don't rely on serialized field which may be stale
        monitorTimer -= Time.deltaTime;
        if (monitorTimer > 0f)
            return;

        monitorTimer = monitorInterval;

        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        if (enemies.Length == 0)
            return;

        // Log as Warning so it shows up even with console filters
        if (enemies.Length != lastEnemyCount)
        {
            lastEnemyCount = enemies.Length;
            Debug.LogWarning($"[EnemyMonitor] {enemies.Length} enemies now alive");
        }

        foreach (EnemyController enemy in enemies)
        {
            if (enemy == null) continue;

            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            Vector3 pos = enemy.transform.position;
            string velStr = rb != null
                ? $"vel=({rb.linearVelocity.x:F1},{rb.linearVelocity.y:F1}) sim={rb.simulated} body={rb.bodyType} grav={rb.gravityScale}"
                : "NO_RB";

            BaseEnemyMovement mov = enemy.GetComponent<BaseEnemyMovement>();
            string movStr = mov != null
                ? $"grnd={mov.IsGrounded} wall={mov.IsAtWall} ledge={mov.IsAtLedge}"
                : "no_mov";

            EnemySensors sens = enemy.GetComponent<EnemySensors>();
            string sensStr = sens != null
                ? $"target={sens.HasTarget}"
                : "no_sens";

            Debug.LogWarning($"[EnemyMonitor] {enemy.gameObject.name} pos=({pos.x:F1},{pos.y:F1}) state={enemy.CurrentState} | {velStr} | {movStr} | {sensStr}");
        }
    }

    [ContextMenu("Run Enemy Diagnostics")]
    public void RunDiagnostics()
    {
        Debug.Log("<color=cyan>=== ENEMY DIAGNOSTIC REPORT ===</color>");

        CheckTimeScale();
        CheckLayers();
        CheckPlayer();
        CheckDamageNumberSpawner();
        CheckEnemies();

        Debug.Log("<color=cyan>=== END DIAGNOSTIC REPORT ===</color>");
    }

    private void CheckTimeScale()
    {
        if (Mathf.Approximately(Time.timeScale, 0f))
            LogFail("Time.timeScale is 0 — game is paused. Nothing will move.");
        else if (!Mathf.Approximately(Time.timeScale, 1f))
            LogWarn($"Time.timeScale is {Time.timeScale} (not 1.0)");
        else
            LogPass("Time.timeScale is 1.0");
    }

    private void CheckLayers()
    {
        CheckLayerExists("Player", 6);
        CheckLayerExists("Enemy", 13);
        CheckLayerExists("Ground", 7);
        CheckLayerExists("PlayerAttack", 11);
        CheckLayerExists("EnemyHurtbox", 12);
    }

    private void CheckLayerExists(string expectedName, int expectedIndex)
    {
        string layerName = LayerMask.LayerToName(expectedIndex);
        if (string.IsNullOrEmpty(layerName))
            LogFail($"Layer {expectedIndex} has no name (expected \"{expectedName}\"). Sensors using this layer will fail.");
        else if (layerName != expectedName)
            LogWarn($"Layer {expectedIndex} is \"{layerName}\" (expected \"{expectedName}\")");
        else
            LogPass($"Layer {expectedIndex} = \"{expectedName}\"");
    }

    private void CheckPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            LogFail("No GameObject with tag \"Player\" found in scene.");
            return;
        }

        LogPass($"Player found: \"{player.name}\"");

        // Check layer
        string playerLayerName = LayerMask.LayerToName(player.layer);
        int playerLayerMask = 1 << player.layer;

        if (string.IsNullOrEmpty(playerLayerName))
            LogFail($"Player is on layer {player.layer} which has no name. Sensors checking by layer will not find it.");
        else
            LogInfo($"Player layer: {player.layer} (\"{playerLayerName}\"), bitmask: {playerLayerMask}");

        // Check if enemy sensors would find the player
        EnemySensors[] allSensors = FindObjectsByType<EnemySensors>(FindObjectsSortMode.None);
        if (allSensors.Length > 0)
        {
            // We can't read the private targetLayers field directly, but we can check if the player's layer
            // is included in "Player" mask
            int playerMask = LayerMask.GetMask("Player");
            if (playerMask == 0)
                LogFail("LayerMask.GetMask(\"Player\") returns 0 — no layer named \"Player\" exists. Sensor fallback in Start() will fail.");
            else if ((playerMask & (1 << player.layer)) == 0)
                LogFail($"Player is on layer {player.layer}, but \"Player\" layer mask is {playerMask}. Layer mismatch — sensors won't detect player.");
            else
                LogPass("Player layer matches \"Player\" layer mask — sensors should detect it.");
        }

        // Check required components
        if (player.GetComponent<Rigidbody2D>() == null)
            LogWarn("Player has no Rigidbody2D.");

        if (player.GetComponent<Collider2D>() == null)
            LogWarn("Player has no Collider2D.");
    }

    private void CheckDamageNumberSpawner()
    {
        if (DamageNumberSpawner.Instance != null)
            LogPass("DamageNumberSpawner.Instance exists.");
        else
            LogFail("DamageNumberSpawner.Instance is null — damage numbers will not appear.");
    }

    private void CheckEnemies()
    {
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);

        if (enemies.Length == 0)
        {
            LogWarn("No EnemyController instances found in scene.");
            return;
        }

        LogInfo($"Found {enemies.Length} enemy(ies) in scene.");

        foreach (EnemyController enemy in enemies)
        {
            Debug.Log($"<color=cyan>--- Enemy: \"{enemy.gameObject.name}\" ---</color>");
            CheckEnemy(enemy);
        }
    }

    private void CheckEnemy(EnemyController enemy)
    {
        GameObject go = enemy.gameObject;

        // EnemyData
        if (enemy.Data == null)
            LogFail($"  EnemyData is NULL — controller will disable itself.");
        else
            LogPass($"  EnemyData: \"{enemy.Data.enemyName}\" (type: {enemy.Data.enemyType})");

        // Layer
        string layerName = LayerMask.LayerToName(go.layer);
        if (string.IsNullOrEmpty(layerName))
            LogWarn($"  GameObject layer: {go.layer} (unnamed)");
        else
            LogInfo($"  GameObject layer: {go.layer} (\"{layerName}\")");

        // Rigidbody2D
        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            LogFail("  No Rigidbody2D — enemy cannot move or be affected by physics.");
        }
        else
        {
            if (rb.bodyType != RigidbodyType2D.Dynamic)
                LogFail($"  Rigidbody2D bodyType is {rb.bodyType} (must be Dynamic).");
            else
                LogPass("  Rigidbody2D: Dynamic");

            if (!rb.simulated)
                LogFail("  Rigidbody2D.simulated is FALSE — physics completely disabled.");
            else
                LogPass("  Rigidbody2D: simulated=true");

            LogInfo($"  Rigidbody2D: gravityScale={rb.gravityScale}, constraints={rb.constraints}");
        }

        // Collider
        Collider2D col = go.GetComponent<Collider2D>();
        if (col == null)
            LogFail("  No Collider2D — enemy cannot collide or be hit.");
        else if (col.isTrigger)
            LogWarn("  Collider2D.isTrigger is TRUE — may not collide with ground or player.");
        else
            LogPass($"  Collider2D: {col.GetType().Name}, isTrigger=false");

        // Movement
        BaseEnemyMovement movement = go.GetComponent<BaseEnemyMovement>();
        if (enemy.Data != null && enemy.Data.enemyType != EnemyType.Stationary)
        {
            if (movement == null)
                LogFail($"  No movement component for {enemy.Data.enemyType} enemy.");
            else
                LogPass($"  Movement: {movement.GetType().Name}");
        }

        // Sensors
        EnemySensors sensors = go.GetComponent<EnemySensors>();
        if (sensors == null)
            LogFail("  No EnemySensors — enemy cannot detect player.");
        else
            LogPass("  EnemySensors: present");

        // Combat
        EnemyCombat combat = go.GetComponent<EnemyCombat>();
        if (combat == null)
        {
            if (enemy.Data != null && enemy.Data.attacks != null && enemy.Data.attacks.Length > 0)
                LogFail("  No EnemyCombat but EnemyData has attacks configured.");
            else
                LogInfo("  No EnemyCombat (no attacks configured).");
        }
        else
        {
            LogPass("  EnemyCombat: present");
        }

        // HealthSystem
        HealthSystem health = go.GetComponent<HealthSystem>();
        if (health == null)
            LogFail("  No HealthSystem — enemy cannot take damage.");
        else
            LogPass($"  HealthSystem: present (HP: {health.CurrentHealth}/{health.MaxHealth})");

        // SpriteRenderer
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null)
            LogWarn("  No SpriteRenderer — enemy is invisible.");
        else
            LogPass("  SpriteRenderer: present");

        // Current state
        LogInfo($"  Current state: {enemy.CurrentState}");
    }

    private void LogPass(string message)
    {
        Debug.Log($"<color=green>[PASS]</color> {message}");
    }

    private void LogFail(string message)
    {
        Debug.LogError($"<color=red>[FAIL]</color> {message}");
    }

    private void LogWarn(string message)
    {
        Debug.LogWarning($"<color=yellow>[WARN]</color> {message}");
    }

    private void LogInfo(string message)
    {
        Debug.Log($"<color=white>[INFO]</color> {message}");
    }
}
