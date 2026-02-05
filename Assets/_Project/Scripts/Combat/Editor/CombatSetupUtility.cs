#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Editor utility for setting up combat system assets.
/// Access via Tools > Combat > Setup Combat Assets
/// </summary>
public static class CombatSetupUtility
{
    private const string DataPath = "Assets/_Project/Data/Combat";

    [MenuItem("Tools/Combat/Setup Combat Assets")]
    public static void SetupCombatAssets()
    {
        // Create directories if they don't exist
        EnsureDirectoryExists(DataPath);
        EnsureDirectoryExists(DataPath + "/Attacks");
        EnsureDirectoryExists(DataPath + "/Weapons");

        // Create attack data assets
        CreateMeleeAttacks();
        CreateRangedAttacks();
        CreateMagicAttacks();

        // Create weapon data assets
        CreateWeapons();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Combat assets created successfully at: " + DataPath);
    }

    [MenuItem("Tools/Combat/Setup Layers")]
    public static void SetupLayers()
    {
        // Note: Adding layers programmatically requires modifying TagManager asset
        // This just provides instructions
        Debug.Log("Please manually add the following layers in Project Settings > Tags and Layers:\n" +
                  "- Layer 11: PlayerAttack\n" +
                  "- Layer 12: EnemyHurtbox\n\n" +
                  "Then configure Physics 2D collision matrix so PlayerAttack only hits EnemyHurtbox.");
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string folderName = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    #region Melee Attacks

    private static void CreateMeleeAttacks()
    {
        // Forward Slash
        var forwardSlash = CreateAttackData("Sword_Forward", "Forward Slash");
        forwardSlash.direction = AttackDirection.Forward;
        forwardSlash.weaponType = WeaponType.Melee;
        forwardSlash.baseDamage = 15f;
        forwardSlash.knockbackForce = 5f;
        forwardSlash.hitboxSize = new Vector2(1.8f, 1.2f);
        forwardSlash.hitboxOffset = new Vector2(1.2f, 0f);
        forwardSlash.windUpDuration = 0f;
        forwardSlash.activeDuration = 0.15f;
        forwardSlash.recoveryDuration = 0.25f;
        forwardSlash.comboWindowDuration = 0.3f;
        forwardSlash.animationTrigger = "Attack_Forward";
        EditorUtility.SetDirty(forwardSlash);

        // Up Slash
        var upSlash = CreateAttackData("Sword_Up", "Up Slash");
        upSlash.direction = AttackDirection.Up;
        upSlash.weaponType = WeaponType.Melee;
        upSlash.baseDamage = 12f;
        upSlash.knockbackForce = 6f;
        upSlash.knockbackDirection = new Vector2(0.5f, 1f);
        upSlash.hitboxSize = new Vector2(1.2f, 1.8f);
        upSlash.hitboxOffset = new Vector2(0f, 1.2f);
        upSlash.windUpDuration = 0f;
        upSlash.activeDuration = 0.15f;
        upSlash.recoveryDuration = 0.25f;
        upSlash.animationTrigger = "Attack_Up";
        EditorUtility.SetDirty(upSlash);

        // Down Thrust (Pogo)
        var downThrust = CreateAttackData("Sword_Down", "Down Thrust");
        downThrust.direction = AttackDirection.Down;
        downThrust.weaponType = WeaponType.Melee;
        downThrust.baseDamage = 12f;
        downThrust.knockbackForce = 4f;
        downThrust.knockbackDirection = new Vector2(0f, -1f);
        downThrust.hitboxSize = new Vector2(1f, 1.5f);
        downThrust.hitboxOffset = new Vector2(0f, -1f);
        downThrust.windUpDuration = 0f;
        downThrust.activeDuration = 0.2f;
        downThrust.recoveryDuration = 0.15f;
        downThrust.canUseInAir = true;
        downThrust.pogoOnDownHit = true;
        downThrust.pogoForce = 12f;
        downThrust.animationTrigger = "Attack_Down";
        EditorUtility.SetDirty(downThrust);

        // Combo second hit
        var forwardSlash2 = CreateAttackData("Sword_Forward_2", "Forward Slash 2");
        forwardSlash2.direction = AttackDirection.Forward;
        forwardSlash2.weaponType = WeaponType.Melee;
        forwardSlash2.baseDamage = 20f;
        forwardSlash2.knockbackForce = 8f;
        forwardSlash2.hitboxSize = new Vector2(2f, 1.4f);
        forwardSlash2.hitboxOffset = new Vector2(1.4f, 0f);
        forwardSlash2.windUpDuration = 0.05f;
        forwardSlash2.activeDuration = 0.15f;
        forwardSlash2.recoveryDuration = 0.35f;
        forwardSlash2.animationTrigger = "Attack_Forward_2";
        EditorUtility.SetDirty(forwardSlash2);

        // Link combo
        forwardSlash.comboNextAttack = forwardSlash2;
        EditorUtility.SetDirty(forwardSlash);
    }

    #endregion

    #region Ranged Attacks

    private static void CreateRangedAttacks()
    {
        // Forward Throw
        var forwardThrow = CreateAttackData("Knife_Forward", "Throw Knife");
        forwardThrow.direction = AttackDirection.Forward;
        forwardThrow.weaponType = WeaponType.Ranged;
        forwardThrow.baseDamage = 8f;
        forwardThrow.knockbackForce = 2f;
        forwardThrow.hitboxSize = new Vector2(0.5f, 0.3f);
        forwardThrow.hitboxOffset = new Vector2(0.8f, 0f);
        forwardThrow.windUpDuration = 0.1f;
        forwardThrow.activeDuration = 0.1f;
        forwardThrow.recoveryDuration = 0.3f;
        forwardThrow.projectileSpeed = 18f;
        forwardThrow.animationTrigger = "Throw_Forward";
        // Note: projectilePrefab must be assigned in editor
        EditorUtility.SetDirty(forwardThrow);

        // Up Throw
        var upThrow = CreateAttackData("Knife_Up", "Throw Knife Up");
        upThrow.direction = AttackDirection.Up;
        upThrow.weaponType = WeaponType.Ranged;
        upThrow.baseDamage = 8f;
        upThrow.knockbackForce = 2f;
        upThrow.hitboxSize = new Vector2(0.3f, 0.5f);
        upThrow.hitboxOffset = new Vector2(0f, 0.8f);
        upThrow.windUpDuration = 0.1f;
        upThrow.activeDuration = 0.1f;
        upThrow.recoveryDuration = 0.3f;
        upThrow.projectileSpeed = 18f;
        upThrow.animationTrigger = "Throw_Up";
        EditorUtility.SetDirty(upThrow);

        // Down Throw
        var downThrow = CreateAttackData("Knife_Down", "Throw Knife Down");
        downThrow.direction = AttackDirection.Down;
        downThrow.weaponType = WeaponType.Ranged;
        downThrow.baseDamage = 8f;
        downThrow.knockbackForce = 2f;
        downThrow.hitboxSize = new Vector2(0.3f, 0.5f);
        downThrow.hitboxOffset = new Vector2(0f, -0.8f);
        downThrow.windUpDuration = 0.1f;
        downThrow.activeDuration = 0.1f;
        downThrow.recoveryDuration = 0.3f;
        downThrow.projectileSpeed = 18f;
        downThrow.canUseInAir = true;
        downThrow.animationTrigger = "Throw_Down";
        EditorUtility.SetDirty(downThrow);
    }

    #endregion

    #region Magic Attacks

    private static void CreateMagicAttacks()
    {
        // Fireball Forward
        var fireballForward = CreateAttackData("Magic_Forward", "Fireball");
        fireballForward.direction = AttackDirection.Forward;
        fireballForward.weaponType = WeaponType.Magic;
        fireballForward.baseDamage = 25f;
        fireballForward.knockbackForce = 4f;
        fireballForward.hitboxSize = new Vector2(0.8f, 0.8f);
        fireballForward.hitboxOffset = new Vector2(1f, 0f);
        fireballForward.manaCost = 15f;
        fireballForward.windUpDuration = 0.15f;
        fireballForward.activeDuration = 0.1f;
        fireballForward.recoveryDuration = 0.4f;
        fireballForward.projectileSpeed = 12f;
        fireballForward.animationTrigger = "Cast_Forward";
        EditorUtility.SetDirty(fireballForward);

        // Lightning Up
        var lightningUp = CreateAttackData("Magic_Up", "Lightning Strike");
        lightningUp.direction = AttackDirection.Up;
        lightningUp.weaponType = WeaponType.Magic;
        lightningUp.baseDamage = 20f;
        lightningUp.knockbackForce = 8f;
        lightningUp.knockbackDirection = new Vector2(0f, 1f);
        lightningUp.hitboxSize = new Vector2(1.5f, 3f);
        lightningUp.hitboxOffset = new Vector2(0f, 2f);
        lightningUp.manaCost = 20f;
        lightningUp.windUpDuration = 0.2f;
        lightningUp.activeDuration = 0.2f;
        lightningUp.recoveryDuration = 0.4f;
        lightningUp.animationTrigger = "Cast_Up";
        EditorUtility.SetDirty(lightningUp);

        // Ground Slam Down
        var groundSlam = CreateAttackData("Magic_Down", "Ground Slam");
        groundSlam.direction = AttackDirection.Down;
        groundSlam.weaponType = WeaponType.Magic;
        groundSlam.baseDamage = 30f;
        groundSlam.knockbackForce = 10f;
        groundSlam.knockbackDirection = new Vector2(1f, 0.5f);
        groundSlam.hitboxSize = new Vector2(3f, 1f);
        groundSlam.hitboxOffset = new Vector2(0f, -0.5f);
        groundSlam.manaCost = 25f;
        groundSlam.windUpDuration = 0.25f;
        groundSlam.activeDuration = 0.25f;
        groundSlam.recoveryDuration = 0.5f;
        groundSlam.canUseInAir = false; // Ground only
        groundSlam.animationTrigger = "Cast_Down";
        EditorUtility.SetDirty(groundSlam);
    }

    #endregion

    #region Weapons

    private static void CreateWeapons()
    {
        // Basic Sword
        var basicSword = CreateWeaponData("BasicSword", "Basic Sword");
        basicSword.weaponType = WeaponType.Melee;
        basicSword.forwardAttack = AssetDatabase.LoadAssetAtPath<AttackData>(DataPath + "/Attacks/Sword_Forward.asset");
        basicSword.upAttack = AssetDatabase.LoadAssetAtPath<AttackData>(DataPath + "/Attacks/Sword_Up.asset");
        basicSword.downAttack = AssetDatabase.LoadAssetAtPath<AttackData>(DataPath + "/Attacks/Sword_Down.asset");
        EditorUtility.SetDirty(basicSword);

        // Throwing Knives
        var throwingKnives = CreateWeaponData("ThrowingKnives", "Throwing Knives");
        throwingKnives.weaponType = WeaponType.Ranged;
        throwingKnives.forwardAttack = AssetDatabase.LoadAssetAtPath<AttackData>(DataPath + "/Attacks/Knife_Forward.asset");
        throwingKnives.upAttack = AssetDatabase.LoadAssetAtPath<AttackData>(DataPath + "/Attacks/Knife_Up.asset");
        throwingKnives.downAttack = AssetDatabase.LoadAssetAtPath<AttackData>(DataPath + "/Attacks/Knife_Down.asset");
        EditorUtility.SetDirty(throwingKnives);

        // Magic Staff
        var magicStaff = CreateWeaponData("MagicStaff", "Magic Staff");
        magicStaff.weaponType = WeaponType.Magic;
        magicStaff.forwardAttack = AssetDatabase.LoadAssetAtPath<AttackData>(DataPath + "/Attacks/Magic_Forward.asset");
        magicStaff.upAttack = AssetDatabase.LoadAssetAtPath<AttackData>(DataPath + "/Attacks/Magic_Up.asset");
        magicStaff.downAttack = AssetDatabase.LoadAssetAtPath<AttackData>(DataPath + "/Attacks/Magic_Down.asset");
        EditorUtility.SetDirty(magicStaff);
    }

    #endregion

    #region Helper Methods

    private static AttackData CreateAttackData(string fileName, string displayName)
    {
        string path = DataPath + "/Attacks/" + fileName + ".asset";

        var existing = AssetDatabase.LoadAssetAtPath<AttackData>(path);
        if (existing != null)
        {
            return existing;
        }

        var asset = ScriptableObject.CreateInstance<AttackData>();
        asset.attackName = displayName;
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static WeaponData CreateWeaponData(string fileName, string displayName)
    {
        string path = DataPath + "/Weapons/" + fileName + ".asset";

        var existing = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
        if (existing != null)
        {
            return existing;
        }

        var asset = ScriptableObject.CreateInstance<WeaponData>();
        asset.weaponName = displayName;
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    #endregion
}
#endif
