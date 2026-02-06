#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

/// <summary>
/// Editor tool to generate placeholder sprites and animations for enemy types.
/// Creates simple but visually distinct sprites for Slime, Bat, and Turret.
/// </summary>
public class EnemySpriteGenerator : EditorWindow
{
    private const string SPRITES_PATH = "Assets/_Project/Art/Sprites/Enemies";
    private const string ANIMATIONS_PATH = "Assets/_Project/Art/Animations/Enemies";

    [MenuItem("Tools/ProjectName/Generate Enemy Sprites & Animations")]
    public static void ShowWindow()
    {
        GetWindow<EnemySpriteGenerator>("Enemy Sprite Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Enemy Sprite & Animation Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This tool generates placeholder sprites and animations for:\n" +
            "- Slime (green blob with squash/stretch)\n" +
            "- Bat (purple with wing flap)\n" +
            "- Turret (gray metallic with rotation)",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate All Enemy Assets", GUILayout.Height(40)))
        {
            GenerateAllAssets();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Individual Generation", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Slime")) GenerateSlimeAssets();
        if (GUILayout.Button("Bat")) GenerateBatAssets();
        if (GUILayout.Button("Turret")) GenerateTurretAssets();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (GUILayout.Button("Update Prefabs with Generated Assets", GUILayout.Height(30)))
        {
            UpdatePrefabs();
        }
    }

    private void GenerateAllAssets()
    {
        EnsureDirectories();
        GenerateSlimeAssets();
        GenerateBatAssets();
        GenerateTurretAssets();
        AssetDatabase.Refresh();
        Debug.Log("[EnemySpriteGenerator] All enemy assets generated!");
        EditorUtility.DisplayDialog("Complete", "All enemy sprites and animations generated!", "OK");
    }

    private void EnsureDirectories()
    {
        if (!Directory.Exists(SPRITES_PATH))
        {
            Directory.CreateDirectory(SPRITES_PATH);
        }
        if (!Directory.Exists(ANIMATIONS_PATH))
        {
            Directory.CreateDirectory(ANIMATIONS_PATH);
        }
        if (!Directory.Exists(ANIMATIONS_PATH + "/Slime"))
        {
            Directory.CreateDirectory(ANIMATIONS_PATH + "/Slime");
        }
        if (!Directory.Exists(ANIMATIONS_PATH + "/Bat"))
        {
            Directory.CreateDirectory(ANIMATIONS_PATH + "/Bat");
        }
        if (!Directory.Exists(ANIMATIONS_PATH + "/Turret"))
        {
            Directory.CreateDirectory(ANIMATIONS_PATH + "/Turret");
        }
        AssetDatabase.Refresh();
    }

    #region Slime Generation

    private void GenerateSlimeAssets()
    {
        EnsureDirectories();

        // Generate slime sprite frames (blob shape)
        Sprite[] slimeFrames = new Sprite[4];
        for (int i = 0; i < 4; i++)
        {
            float squash = 1f + Mathf.Sin(i * Mathf.PI / 2) * 0.15f;
            Texture2D tex = CreateSlimeTexture(64, 64, squash);
            string path = $"{SPRITES_PATH}/Slime_Frame_{i}.png";
            SaveTexture(tex, path);
            AssetDatabase.Refresh();
            ConfigureSpriteImporter(path);
            slimeFrames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // Create hurt frame (red tint)
        Texture2D hurtTex = CreateSlimeTexture(64, 64, 1f, true);
        SaveTexture(hurtTex, $"{SPRITES_PATH}/Slime_Hurt.png");
        AssetDatabase.Refresh();
        ConfigureSpriteImporter($"{SPRITES_PATH}/Slime_Hurt.png");

        // Create animation controller
        CreateSlimeAnimator(slimeFrames);

        Debug.Log("[EnemySpriteGenerator] Slime assets generated!");
    }

    private Texture2D CreateSlimeTexture(int width, int height, float squash, bool hurt = false)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color baseColor = hurt ? new Color(1f, 0.3f, 0.3f, 1f) : new Color(0.2f, 0.85f, 0.3f, 1f);
        Color highlightColor = hurt ? new Color(1f, 0.6f, 0.6f, 1f) : new Color(0.4f, 1f, 0.5f, 1f);
        Color shadowColor = hurt ? new Color(0.7f, 0.1f, 0.1f, 1f) : new Color(0.1f, 0.5f, 0.15f, 1f);

        // Clear to transparent
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        // Draw blob shape
        float centerX = width / 2f;
        float centerY = height / 2.5f;
        float radiusX = width * 0.4f * squash;
        float radiusY = height * 0.35f / squash;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = (x - centerX) / radiusX;
                float dy = (y - centerY) / radiusY;
                float dist = dx * dx + dy * dy;

                if (dist < 1f)
                {
                    // Inside blob
                    float shade = 1f - dist;
                    Color pixelColor;

                    // Highlight on top-left
                    if (dx < -0.2f && dy > 0.2f)
                        pixelColor = Color.Lerp(baseColor, highlightColor, shade * 0.8f);
                    // Shadow on bottom-right
                    else if (dx > 0.2f || dy < -0.2f)
                        pixelColor = Color.Lerp(baseColor, shadowColor, (1f - shade) * 0.5f);
                    else
                        pixelColor = baseColor;

                    // Soft edge
                    if (dist > 0.8f)
                        pixelColor.a = (1f - dist) / 0.2f;

                    pixels[y * width + x] = pixelColor;
                }
            }
        }

        // Add cute eyes
        DrawEye(pixels, width, height, (int)(centerX - 8), (int)(centerY + 10), 4);
        DrawEye(pixels, width, height, (int)(centerX + 8), (int)(centerY + 10), 4);

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private void DrawEye(Color[] pixels, int width, int height, int cx, int cy, int radius)
    {
        // White of eye
        for (int y = cy - radius; y <= cy + radius; y++)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (dist <= radius)
                        pixels[y * width + x] = Color.white;
                }
            }
        }
        // Pupil
        int pupilRadius = radius / 2;
        for (int y = cy - pupilRadius; y <= cy + pupilRadius; y++)
        {
            for (int x = cx - pupilRadius; x <= cx + pupilRadius; x++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (dist <= pupilRadius)
                        pixels[y * width + x] = Color.black;
                }
            }
        }
    }

    private void CreateSlimeAnimator(Sprite[] frames)
    {
        string controllerPath = $"{ANIMATIONS_PATH}/Slime/Slime.controller";

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Add parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

        // Create animations
        AnimationClip idleClip = CreateSpriteAnimation("Slime_Idle", frames, 4f, true);
        AnimationClip moveClip = CreateSpriteAnimation("Slime_Move", frames, 8f, true);
        AnimationClip hurtClip = CreateHurtAnimation("Slime_Hurt", $"{SPRITES_PATH}/Slime_Hurt.png");
        AnimationClip attackClip = CreateSquashAnimation("Slime_Attack", frames[0]);
        AnimationClip dieClip = CreateDeathAnimation("Slime_Die", frames[0]);

        AssetDatabase.CreateAsset(idleClip, $"{ANIMATIONS_PATH}/Slime/Slime_Idle.anim");
        AssetDatabase.CreateAsset(moveClip, $"{ANIMATIONS_PATH}/Slime/Slime_Move.anim");
        AssetDatabase.CreateAsset(hurtClip, $"{ANIMATIONS_PATH}/Slime/Slime_Hurt.anim");
        AssetDatabase.CreateAsset(attackClip, $"{ANIMATIONS_PATH}/Slime/Slime_Attack.anim");
        AssetDatabase.CreateAsset(dieClip, $"{ANIMATIONS_PATH}/Slime/Slime_Die.anim");

        // Get the root state machine
        var rootStateMachine = controller.layers[0].stateMachine;

        // Add states
        var idleState = rootStateMachine.AddState("Idle");
        idleState.motion = idleClip;
        var moveState = rootStateMachine.AddState("Move");
        moveState.motion = moveClip;
        var hurtState = rootStateMachine.AddState("Hurt");
        hurtState.motion = hurtClip;
        var attackState = rootStateMachine.AddState("Attack");
        attackState.motion = attackClip;
        var dieState = rootStateMachine.AddState("Die");
        dieState.motion = dieClip;

        // Set default state
        rootStateMachine.defaultState = idleState;

        // Add transitions
        var idleToMove = idleState.AddTransition(moveState);
        idleToMove.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        idleToMove.hasExitTime = false;
        idleToMove.duration = 0.1f;

        var moveToIdle = moveState.AddTransition(idleState);
        moveToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        moveToIdle.hasExitTime = false;
        moveToIdle.duration = 0.1f;

        // Hurt transitions (from any state)
        var anyToHurt = rootStateMachine.AddAnyStateTransition(hurtState);
        anyToHurt.AddCondition(AnimatorConditionMode.If, 0, "Hurt");
        anyToHurt.hasExitTime = false;
        anyToHurt.duration = 0f;

        var hurtToIdle = hurtState.AddTransition(idleState);
        hurtToIdle.hasExitTime = true;
        hurtToIdle.exitTime = 1f;
        hurtToIdle.duration = 0.1f;

        // Attack transitions
        var anyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
        anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        anyToAttack.hasExitTime = false;
        anyToAttack.duration = 0f;

        var attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 1f;
        attackToIdle.duration = 0.1f;

        // Die transition
        var anyToDie = rootStateMachine.AddAnyStateTransition(dieState);
        anyToDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
        anyToDie.hasExitTime = false;
        anyToDie.duration = 0f;

        AssetDatabase.SaveAssets();
    }

    #endregion

    #region Bat Generation

    private void GenerateBatAssets()
    {
        EnsureDirectories();

        // Generate bat sprite frames (wing positions)
        Sprite[] batFrames = new Sprite[4];
        for (int i = 0; i < 4; i++)
        {
            float wingAngle = Mathf.Sin(i * Mathf.PI / 2) * 30f;
            Texture2D tex = CreateBatTexture(64, 64, wingAngle);
            string path = $"{SPRITES_PATH}/Bat_Frame_{i}.png";
            SaveTexture(tex, path);
            AssetDatabase.Refresh();
            ConfigureSpriteImporter(path);
            batFrames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // Create hurt frame
        Texture2D hurtTex = CreateBatTexture(64, 64, 0f, true);
        SaveTexture(hurtTex, $"{SPRITES_PATH}/Bat_Hurt.png");
        AssetDatabase.Refresh();
        ConfigureSpriteImporter($"{SPRITES_PATH}/Bat_Hurt.png");

        // Create animation controller
        CreateBatAnimator(batFrames);

        Debug.Log("[EnemySpriteGenerator] Bat assets generated!");
    }

    private Texture2D CreateBatTexture(int width, int height, float wingAngle, bool hurt = false)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color bodyColor = hurt ? new Color(1f, 0.3f, 0.3f, 1f) : new Color(0.4f, 0.2f, 0.5f, 1f);
        Color wingColor = hurt ? new Color(0.8f, 0.2f, 0.2f, 1f) : new Color(0.3f, 0.15f, 0.4f, 1f);
        Color eyeColor = new Color(1f, 0.8f, 0f, 1f);

        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        float centerX = width / 2f;
        float centerY = height / 2f;

        // Draw body (oval)
        float bodyRadiusX = 10f;
        float bodyRadiusY = 8f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = (x - centerX) / bodyRadiusX;
                float dy = (y - centerY) / bodyRadiusY;
                float dist = dx * dx + dy * dy;

                if (dist < 1f)
                {
                    pixels[y * width + x] = bodyColor;
                }
            }
        }

        // Draw wings
        float wingExtension = 1f + Mathf.Abs(wingAngle) / 60f;
        DrawWing(pixels, width, height, centerX - 8, centerY, -wingAngle, wingExtension, wingColor);
        DrawWing(pixels, width, height, centerX + 8, centerY, wingAngle, wingExtension, wingColor);

        // Draw ears
        DrawTriangle(pixels, width, height, (int)centerX - 5, (int)centerY + 8, 4, 8, bodyColor);
        DrawTriangle(pixels, width, height, (int)centerX + 5, (int)centerY + 8, 4, 8, bodyColor);

        // Draw eyes
        DrawCircle(pixels, width, height, (int)centerX - 4, (int)centerY + 2, 3, eyeColor);
        DrawCircle(pixels, width, height, (int)centerX + 4, (int)centerY + 2, 3, eyeColor);
        DrawCircle(pixels, width, height, (int)centerX - 4, (int)centerY + 2, 1, Color.black);
        DrawCircle(pixels, width, height, (int)centerX + 4, (int)centerY + 2, 1, Color.black);

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private void DrawWing(Color[] pixels, int width, int height, float startX, float startY, float angle, float extension, Color color)
    {
        float rad = angle * Mathf.Deg2Rad;
        float wingLength = 18f * extension;
        float wingWidth = 12f;

        for (int i = 0; i < (int)wingLength; i++)
        {
            float t = i / wingLength;
            float px = startX + Mathf.Cos(rad - Mathf.PI / 2) * i * (startX < width / 2 ? -1 : 1);
            float py = startY + Mathf.Sin(rad - Mathf.PI / 2) * i * 0.3f;

            float segWidth = wingWidth * (1f - t * 0.7f);
            for (int w = -(int)segWidth / 2; w <= (int)segWidth / 2; w++)
            {
                int x = (int)(px + w);
                int y = (int)(py);
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    pixels[y * width + x] = color;
                }
            }
        }
    }

    private void DrawTriangle(Color[] pixels, int width, int height, int cx, int cy, int baseWidth, int triHeight, Color color)
    {
        for (int y = 0; y < triHeight; y++)
        {
            float t = (float)y / triHeight;
            int rowWidth = (int)(baseWidth * (1f - t));
            for (int x = -rowWidth / 2; x <= rowWidth / 2; x++)
            {
                int px = cx + x;
                int py = cy + y;
                if (px >= 0 && px < width && py >= 0 && py < height)
                {
                    pixels[py * width + px] = color;
                }
            }
        }
    }

    private void DrawCircle(Color[] pixels, int width, int height, int cx, int cy, int radius, Color color)
    {
        for (int y = cy - radius; y <= cy + radius; y++)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (dist <= radius)
                        pixels[y * width + x] = color;
                }
            }
        }
    }

    private void CreateBatAnimator(Sprite[] frames)
    {
        string controllerPath = $"{ANIMATIONS_PATH}/Bat/Bat.controller";

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

        AnimationClip idleClip = CreateSpriteAnimation("Bat_Idle", frames, 8f, true);
        AnimationClip moveClip = CreateSpriteAnimation("Bat_Move", frames, 12f, true);
        AnimationClip hurtClip = CreateHurtAnimation("Bat_Hurt", $"{SPRITES_PATH}/Bat_Hurt.png");
        AnimationClip attackClip = CreateSpriteAnimation("Bat_Attack", frames, 16f, false);
        AnimationClip dieClip = CreateDeathAnimation("Bat_Die", frames[0]);

        AssetDatabase.CreateAsset(idleClip, $"{ANIMATIONS_PATH}/Bat/Bat_Idle.anim");
        AssetDatabase.CreateAsset(moveClip, $"{ANIMATIONS_PATH}/Bat/Bat_Move.anim");
        AssetDatabase.CreateAsset(hurtClip, $"{ANIMATIONS_PATH}/Bat/Bat_Hurt.anim");
        AssetDatabase.CreateAsset(attackClip, $"{ANIMATIONS_PATH}/Bat/Bat_Attack.anim");
        AssetDatabase.CreateAsset(dieClip, $"{ANIMATIONS_PATH}/Bat/Bat_Die.anim");

        var rootStateMachine = controller.layers[0].stateMachine;

        var idleState = rootStateMachine.AddState("Idle");
        idleState.motion = idleClip;
        var moveState = rootStateMachine.AddState("Move");
        moveState.motion = moveClip;
        var hurtState = rootStateMachine.AddState("Hurt");
        hurtState.motion = hurtClip;
        var attackState = rootStateMachine.AddState("Attack");
        attackState.motion = attackClip;
        var dieState = rootStateMachine.AddState("Die");
        dieState.motion = dieClip;

        rootStateMachine.defaultState = idleState;

        var idleToMove = idleState.AddTransition(moveState);
        idleToMove.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        idleToMove.hasExitTime = false;
        idleToMove.duration = 0.1f;

        var moveToIdle = moveState.AddTransition(idleState);
        moveToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        moveToIdle.hasExitTime = false;
        moveToIdle.duration = 0.1f;

        var anyToHurt = rootStateMachine.AddAnyStateTransition(hurtState);
        anyToHurt.AddCondition(AnimatorConditionMode.If, 0, "Hurt");
        anyToHurt.hasExitTime = false;
        anyToHurt.duration = 0f;

        var hurtToIdle = hurtState.AddTransition(idleState);
        hurtToIdle.hasExitTime = true;
        hurtToIdle.exitTime = 1f;
        hurtToIdle.duration = 0.1f;

        var anyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
        anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        anyToAttack.hasExitTime = false;
        anyToAttack.duration = 0f;

        var attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 1f;
        attackToIdle.duration = 0.1f;

        var anyToDie = rootStateMachine.AddAnyStateTransition(dieState);
        anyToDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
        anyToDie.hasExitTime = false;
        anyToDie.duration = 0f;

        AssetDatabase.SaveAssets();
    }

    #endregion

    #region Turret Generation

    private void GenerateTurretAssets()
    {
        EnsureDirectories();

        // Generate turret sprite (base + cannon)
        Texture2D baseTex = CreateTurretTexture(64, 64);
        SaveTexture(baseTex, $"{SPRITES_PATH}/Turret_Base.png");
        AssetDatabase.Refresh();
        ConfigureSpriteImporter($"{SPRITES_PATH}/Turret_Base.png");

        // Hurt texture
        Texture2D hurtTex = CreateTurretTexture(64, 64, true);
        SaveTexture(hurtTex, $"{SPRITES_PATH}/Turret_Hurt.png");
        AssetDatabase.Refresh();
        ConfigureSpriteImporter($"{SPRITES_PATH}/Turret_Hurt.png");

        // Create animation controller
        CreateTurretAnimator();

        Debug.Log("[EnemySpriteGenerator] Turret assets generated!");
    }

    private Texture2D CreateTurretTexture(int width, int height, bool hurt = false)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color baseColor = hurt ? new Color(1f, 0.4f, 0.4f, 1f) : new Color(0.5f, 0.5f, 0.55f, 1f);
        Color darkColor = hurt ? new Color(0.7f, 0.2f, 0.2f, 1f) : new Color(0.3f, 0.3f, 0.35f, 1f);
        Color highlightColor = hurt ? new Color(1f, 0.6f, 0.6f, 1f) : new Color(0.7f, 0.7f, 0.75f, 1f);
        Color cannonColor = hurt ? new Color(0.8f, 0.3f, 0.3f, 1f) : new Color(0.4f, 0.4f, 0.45f, 1f);
        Color eyeColor = new Color(1f, 0f, 0f, 1f);

        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        float centerX = width / 2f;
        float baseY = height * 0.3f;

        // Draw base (trapezoid shape)
        for (int y = 0; y < (int)(height * 0.4f); y++)
        {
            float t = (float)y / (height * 0.4f);
            int rowWidth = (int)(20 + t * 10);
            for (int x = -rowWidth; x <= rowWidth; x++)
            {
                int px = (int)centerX + x;
                int py = y + 2;
                if (px >= 0 && px < width && py >= 0 && py < height)
                {
                    Color c = (x < -rowWidth + 3) ? highlightColor :
                              (x > rowWidth - 3) ? darkColor : baseColor;
                    pixels[py * width + px] = c;
                }
            }
        }

        // Draw dome/head
        float domeY = height * 0.4f;
        float domeRadius = 15f;
        for (int y = 0; y < (int)domeRadius; y++)
        {
            for (int x = -(int)domeRadius; x <= (int)domeRadius; x++)
            {
                float dx = x / domeRadius;
                float dy = y / domeRadius;
                if (dx * dx + dy * dy <= 1f)
                {
                    int px = (int)centerX + x;
                    int py = (int)domeY + y;
                    if (px >= 0 && px < width && py >= 0 && py < height)
                    {
                        Color c = (x < -5) ? highlightColor :
                                  (x > 5) ? darkColor : baseColor;
                        pixels[py * width + px] = c;
                    }
                }
            }
        }

        // Draw cannon
        int cannonLength = 20;
        int cannonWidth = 6;
        int cannonStartY = (int)(domeY + domeRadius / 2);
        for (int i = 0; i < cannonLength; i++)
        {
            for (int w = -cannonWidth / 2; w <= cannonWidth / 2; w++)
            {
                int px = (int)centerX + cannonLength / 2 + i;
                int py = cannonStartY + w;
                if (px >= 0 && px < width && py >= 0 && py < height)
                {
                    pixels[py * width + px] = cannonColor;
                }
            }
        }

        // Draw targeting eye
        DrawCircle(pixels, width, height, (int)centerX, (int)(domeY + 8), 5, Color.black);
        DrawCircle(pixels, width, height, (int)centerX, (int)(domeY + 8), 3, eyeColor);

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private void CreateTurretAnimator()
    {
        string controllerPath = $"{ANIMATIONS_PATH}/Turret/Turret.controller";

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

        Sprite baseSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/Turret_Base.png");

        AnimationClip idleClip = CreateTurretIdleAnimation("Turret_Idle");
        AnimationClip hurtClip = CreateHurtAnimation("Turret_Hurt", $"{SPRITES_PATH}/Turret_Hurt.png");
        AnimationClip attackClip = CreateTurretAttackAnimation("Turret_Attack");
        AnimationClip dieClip = CreateDeathAnimation("Turret_Die", baseSprite);

        AssetDatabase.CreateAsset(idleClip, $"{ANIMATIONS_PATH}/Turret/Turret_Idle.anim");
        AssetDatabase.CreateAsset(hurtClip, $"{ANIMATIONS_PATH}/Turret/Turret_Hurt.anim");
        AssetDatabase.CreateAsset(attackClip, $"{ANIMATIONS_PATH}/Turret/Turret_Attack.anim");
        AssetDatabase.CreateAsset(dieClip, $"{ANIMATIONS_PATH}/Turret/Turret_Die.anim");

        var rootStateMachine = controller.layers[0].stateMachine;

        var idleState = rootStateMachine.AddState("Idle");
        idleState.motion = idleClip;
        var hurtState = rootStateMachine.AddState("Hurt");
        hurtState.motion = hurtClip;
        var attackState = rootStateMachine.AddState("Attack");
        attackState.motion = attackClip;
        var dieState = rootStateMachine.AddState("Die");
        dieState.motion = dieClip;

        rootStateMachine.defaultState = idleState;

        var anyToHurt = rootStateMachine.AddAnyStateTransition(hurtState);
        anyToHurt.AddCondition(AnimatorConditionMode.If, 0, "Hurt");
        anyToHurt.hasExitTime = false;
        anyToHurt.duration = 0f;

        var hurtToIdle = hurtState.AddTransition(idleState);
        hurtToIdle.hasExitTime = true;
        hurtToIdle.exitTime = 1f;
        hurtToIdle.duration = 0.1f;

        var anyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
        anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        anyToAttack.hasExitTime = false;
        anyToAttack.duration = 0f;

        var attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 1f;
        attackToIdle.duration = 0.1f;

        var anyToDie = rootStateMachine.AddAnyStateTransition(dieState);
        anyToDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
        anyToDie.hasExitTime = false;
        anyToDie.duration = 0f;

        AssetDatabase.SaveAssets();
    }

    #endregion

    #region Animation Helpers

    private AnimationClip CreateSpriteAnimation(string name, Sprite[] sprites, float fps, bool loop)
    {
        AnimationClip clip = new AnimationClip();
        clip.name = name;
        clip.frameRate = fps;

        EditorCurveBinding spriteBinding = new EditorCurveBinding();
        spriteBinding.type = typeof(SpriteRenderer);
        spriteBinding.path = "";
        spriteBinding.propertyName = "m_Sprite";

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];
        float frameTime = 1f / fps;

        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe();
            keyframes[i].time = i * frameTime;
            keyframes[i].value = sprites[i];
        }

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        return clip;
    }

    private AnimationClip CreateHurtAnimation(string name, string hurtSpritePath)
    {
        AnimationClip clip = new AnimationClip();
        clip.name = name;
        clip.frameRate = 12f;

        Sprite hurtSprite = AssetDatabase.LoadAssetAtPath<Sprite>(hurtSpritePath);

        // Sprite change
        EditorCurveBinding spriteBinding = new EditorCurveBinding();
        spriteBinding.type = typeof(SpriteRenderer);
        spriteBinding.path = "";
        spriteBinding.propertyName = "m_Sprite";

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[1];
        keyframes[0].time = 0f;
        keyframes[0].value = hurtSprite;

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);

        // Flash/shake effect via position
        AnimationCurve shakeX = new AnimationCurve();
        shakeX.AddKey(0f, 0f);
        shakeX.AddKey(0.05f, 0.1f);
        shakeX.AddKey(0.1f, -0.1f);
        shakeX.AddKey(0.15f, 0.05f);
        shakeX.AddKey(0.2f, 0f);

        clip.SetCurve("", typeof(Transform), "localPosition.x", shakeX);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        return clip;
    }

    private AnimationClip CreateSquashAnimation(string name, Sprite sprite)
    {
        AnimationClip clip = new AnimationClip();
        clip.name = name;
        clip.frameRate = 12f;

        // Scale animation for squash attack
        AnimationCurve scaleX = new AnimationCurve();
        scaleX.AddKey(0f, 1f);
        scaleX.AddKey(0.1f, 1.3f);
        scaleX.AddKey(0.2f, 0.8f);
        scaleX.AddKey(0.4f, 1f);

        AnimationCurve scaleY = new AnimationCurve();
        scaleY.AddKey(0f, 1f);
        scaleY.AddKey(0.1f, 0.7f);
        scaleY.AddKey(0.2f, 1.2f);
        scaleY.AddKey(0.4f, 1f);

        clip.SetCurve("", typeof(Transform), "localScale.x", scaleX);
        clip.SetCurve("", typeof(Transform), "localScale.y", scaleY);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        return clip;
    }

    private AnimationClip CreateTurretIdleAnimation(string name)
    {
        AnimationClip clip = new AnimationClip();
        clip.name = name;
        clip.frameRate = 12f;

        // Subtle scanning rotation
        AnimationCurve rotZ = new AnimationCurve();
        rotZ.AddKey(0f, 0f);
        rotZ.AddKey(1f, 5f);
        rotZ.AddKey(2f, 0f);
        rotZ.AddKey(3f, -5f);
        rotZ.AddKey(4f, 0f);

        clip.SetCurve("", typeof(Transform), "localEulerAngles.z", rotZ);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        return clip;
    }

    private AnimationClip CreateTurretAttackAnimation(string name)
    {
        AnimationClip clip = new AnimationClip();
        clip.name = name;
        clip.frameRate = 12f;

        // Recoil animation
        AnimationCurve posX = new AnimationCurve();
        posX.AddKey(0f, 0f);
        posX.AddKey(0.05f, -0.15f);
        posX.AddKey(0.3f, 0f);

        clip.SetCurve("", typeof(Transform), "localPosition.x", posX);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        return clip;
    }

    private AnimationClip CreateDeathAnimation(string name, Sprite sprite)
    {
        AnimationClip clip = new AnimationClip();
        clip.name = name;
        clip.frameRate = 12f;

        // Fade out and fall
        AnimationCurve alpha = new AnimationCurve();
        alpha.AddKey(0f, 1f);
        alpha.AddKey(0.5f, 0.5f);
        alpha.AddKey(1f, 0f);

        AnimationCurve rotZ = new AnimationCurve();
        rotZ.AddKey(0f, 0f);
        rotZ.AddKey(1f, 45f);

        AnimationCurve scaleX = new AnimationCurve();
        scaleX.AddKey(0f, 1f);
        scaleX.AddKey(0.5f, 1.2f);
        scaleX.AddKey(1f, 0.5f);

        AnimationCurve scaleY = new AnimationCurve();
        scaleY.AddKey(0f, 1f);
        scaleY.AddKey(0.5f, 1.2f);
        scaleY.AddKey(1f, 0.5f);

        clip.SetCurve("", typeof(SpriteRenderer), "m_Color.a", alpha);
        clip.SetCurve("", typeof(Transform), "localEulerAngles.z", rotZ);
        clip.SetCurve("", typeof(Transform), "localScale.x", scaleX);
        clip.SetCurve("", typeof(Transform), "localScale.y", scaleY);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        return clip;
    }

    #endregion

    #region Utility

    private void SaveTexture(Texture2D tex, string path)
    {
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        DestroyImmediate(tex);
    }

    private void ConfigureSpriteImporter(string path)
    {
        AssetDatabase.ImportAsset(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 64;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    private void UpdatePrefabs()
    {
        UpdatePrefab("Slime", $"{SPRITES_PATH}/Slime_Frame_0.png", $"{ANIMATIONS_PATH}/Slime/Slime.controller");
        UpdatePrefab("Bat", $"{SPRITES_PATH}/Bat_Frame_0.png", $"{ANIMATIONS_PATH}/Bat/Bat.controller");
        UpdatePrefab("Turret", $"{SPRITES_PATH}/Turret_Base.png", $"{ANIMATIONS_PATH}/Turret/Turret.controller");

        AssetDatabase.SaveAssets();
        Debug.Log("[EnemySpriteGenerator] Prefabs updated with generated assets!");
        EditorUtility.DisplayDialog("Complete", "Enemy prefabs updated with sprites and animations!", "OK");
    }

    private void UpdatePrefab(string enemyName, string spritePath, string controllerPath)
    {
        string prefabPath = $"Assets/_Project/Prefabs/Enemies/{enemyName}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogWarning($"[EnemySpriteGenerator] Prefab not found: {prefabPath}");
            return;
        }

        // Load prefab for editing
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        // Update SpriteRenderer
        SpriteRenderer sr = prefabRoot.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null)
            {
                sr.sprite = sprite;
                sr.color = Color.white; // Reset color since sprites now have their own colors
            }
        }

        // Add or update Animator
        Animator animator = prefabRoot.GetComponent<Animator>();
        if (animator == null)
        {
            animator = prefabRoot.AddComponent<Animator>();
        }

        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        if (controller != null)
        {
            animator.runtimeAnimatorController = controller;
        }

        // Update EnemyController reference to animator
        EnemyController enemyController = prefabRoot.GetComponent<EnemyController>();
        if (enemyController != null)
        {
            SerializedObject so = new SerializedObject(enemyController);
            SerializedProperty animProp = so.FindProperty("animator");
            if (animProp != null)
            {
                animProp.objectReferenceValue = animator;
                so.ApplyModifiedProperties();
            }
        }

        // Save prefab
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        Debug.Log($"[EnemySpriteGenerator] Updated {enemyName} prefab");
    }

    #endregion
}
#endif
