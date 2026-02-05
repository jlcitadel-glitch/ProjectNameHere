#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor wizard to create sample skill system content.
/// Creates Beginner job, starter skills, and skill tree layout.
/// </summary>
public class SkillSystemSetupWizard : EditorWindow
{
    private const string BASE_PATH = "Assets/_Project/ScriptableObjects/Skills";
    private const string JOBS_PATH = BASE_PATH + "/Jobs";
    private const string SKILLS_PATH = BASE_PATH + "/Skills";
    private const string TREES_PATH = BASE_PATH + "/SkillTrees";

    [MenuItem("Tools/ProjectName/Skill System Setup Wizard")]
    public static void ShowWindow()
    {
        GetWindow<SkillSystemSetupWizard>("Skill System Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Skill System Setup Wizard", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This wizard creates sample content for the skill system:\n" +
            "- Beginner, Warrior, Mage, Rogue jobs\n" +
            "- Starter skills for each job\n" +
            "- Skill tree layouts\n\n" +
            "Existing assets with the same names will be overwritten.",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Create All Sample Content", GUILayout.Height(40)))
        {
            CreateAllContent();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Individual Creation", EditorStyles.boldLabel);

        if (GUILayout.Button("Create Jobs Only"))
        {
            CreateJobs();
        }

        if (GUILayout.Button("Create Skills Only"))
        {
            CreateSkills();
        }

        if (GUILayout.Button("Create Skill Trees Only"))
        {
            CreateSkillTrees();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Configure SkillManager"))
        {
            ConfigureSkillManager();
        }
    }

    private void CreateAllContent()
    {
        EnsureDirectories();
        CreateJobs();
        CreateSkills();
        CreateSkillTrees();
        LinkJobsToTrees();
        ConfigureSkillManager();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Complete", "Sample skill system content created successfully!", "OK");
    }

    private void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder(BASE_PATH))
        {
            AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "Skills");
        }
        if (!AssetDatabase.IsValidFolder(JOBS_PATH))
        {
            AssetDatabase.CreateFolder(BASE_PATH, "Jobs");
        }
        if (!AssetDatabase.IsValidFolder(SKILLS_PATH))
        {
            AssetDatabase.CreateFolder(BASE_PATH, "Skills");
        }
        if (!AssetDatabase.IsValidFolder(SKILLS_PATH + "/Beginner"))
        {
            AssetDatabase.CreateFolder(SKILLS_PATH, "Beginner");
        }
        if (!AssetDatabase.IsValidFolder(SKILLS_PATH + "/Warrior"))
        {
            AssetDatabase.CreateFolder(SKILLS_PATH, "Warrior");
        }
        if (!AssetDatabase.IsValidFolder(SKILLS_PATH + "/Mage"))
        {
            AssetDatabase.CreateFolder(SKILLS_PATH, "Mage");
        }
        if (!AssetDatabase.IsValidFolder(SKILLS_PATH + "/Rogue"))
        {
            AssetDatabase.CreateFolder(SKILLS_PATH, "Rogue");
        }
        if (!AssetDatabase.IsValidFolder(TREES_PATH))
        {
            AssetDatabase.CreateFolder(BASE_PATH, "SkillTrees");
        }
    }

    #region Jobs

    private void CreateJobs()
    {
        EnsureDirectories();

        // Beginner
        var beginner = CreateJobAsset("Beginner", "beginner", JobTier.Beginner,
            "The starting class. Learn basic combat skills before choosing your path.",
            new Color(0.7f, 0.7f, 0.7f), 1, 3, 0);

        // First tier jobs
        var warrior = CreateJobAsset("Warrior", "warrior", JobTier.First,
            "Masters of physical combat. High HP and defense with powerful melee attacks.",
            new Color(0.8f, 0.2f, 0.2f), 10, 3, 5);

        var mage = CreateJobAsset("Mage", "mage", JobTier.First,
            "Wielders of arcane power. High MP with devastating magical attacks.",
            new Color(0.2f, 0.4f, 0.9f), 10, 3, 5);

        var rogue = CreateJobAsset("Rogue", "rogue", JobTier.First,
            "Swift and deadly. High critical chance with evasive maneuvers.",
            new Color(0.6f, 0.2f, 0.8f), 10, 3, 5);

        // Link parent/child relationships
        beginner.childJobs = new JobClassData[] { warrior, mage, rogue };
        warrior.parentJob = beginner;
        mage.parentJob = beginner;
        rogue.parentJob = beginner;

        // Job stat modifiers
        warrior.baseHPBonus = 50;
        warrior.baseMPBonus = 0;
        warrior.attackModifier = 1.2f;
        warrior.defenseModifier = 1.3f;

        mage.baseHPBonus = 0;
        mage.baseMPBonus = 50;
        mage.magicModifier = 1.4f;
        mage.defenseModifier = 0.8f;

        rogue.baseHPBonus = 20;
        rogue.baseMPBonus = 20;
        rogue.attackModifier = 1.1f;

        EditorUtility.SetDirty(beginner);
        EditorUtility.SetDirty(warrior);
        EditorUtility.SetDirty(mage);
        EditorUtility.SetDirty(rogue);

        AssetDatabase.SaveAssets();
        Debug.Log("[SkillSystemSetup] Jobs created");
    }

    private JobClassData CreateJobAsset(string name, string id, JobTier tier, string desc, Color color, int reqLevel, int spPerLevel, int bonusSP)
    {
        string path = $"{JOBS_PATH}/{name}.asset";
        var job = AssetDatabase.LoadAssetAtPath<JobClassData>(path);

        if (job == null)
        {
            job = ScriptableObject.CreateInstance<JobClassData>();
            AssetDatabase.CreateAsset(job, path);
        }

        job.jobId = id;
        job.jobName = name;
        job.description = desc;
        job.jobColor = color;
        job.tier = tier;
        job.requiredLevel = reqLevel;
        job.spPerLevel = spPerLevel;
        job.bonusSPOnAdvancement = bonusSP;

        return job;
    }

    #endregion

    #region Skills

    private void CreateSkills()
    {
        EnsureDirectories();

        // Beginner Skills
        CreateBeginnerSkills();

        // Warrior Skills
        CreateWarriorSkills();

        // Mage Skills
        CreateMageSkills();

        // Rogue Skills
        CreateRogueSkills();

        AssetDatabase.SaveAssets();
        Debug.Log("[SkillSystemSetup] Skills created");
    }

    private void CreateBeginnerSkills()
    {
        // Basic Attack Enhancement
        CreateSkillAsset("Beginner/PowerStrike", "power_strike", "Power Strike",
            "A powerful strike that deals {damage} damage.",
            SkillType.Active, DamageType.Physical, "beginner",
            1, 10, 1,
            baseDamage: 15, damagePerLevel: 5,
            manaCost: 5, manaCostPerLevel: 1,
            cooldown: 3f, cooldownReduction: 0.1f,
            tier: 0, nodePos: new Vector2(0, 0));

        // Basic Defense
        CreateSkillAsset("Beginner/Guard", "guard", "Guard",
            "Reduce incoming damage by 30% for {duration}s.",
            SkillType.Buff, DamageType.Physical, "beginner",
            1, 10, 1,
            baseDamage: 0, damagePerLevel: 0,
            manaCost: 10, manaCostPerLevel: 1,
            cooldown: 15f, cooldownReduction: 0.5f,
            duration: 3f, durationPerLevel: 0.2f,
            tier: 0, nodePos: new Vector2(1, 0));

        // Recovery
        CreateSkillAsset("Beginner/Recovery", "recovery", "Recovery",
            "Restore {damage} HP over 5 seconds.",
            SkillType.Active, DamageType.Physical, "beginner",
            3, 10, 1,
            baseDamage: 20, damagePerLevel: 8,
            manaCost: 15, manaCostPerLevel: 2,
            cooldown: 20f, cooldownReduction: 0.5f,
            tier: 1, nodePos: new Vector2(0, 1));

        // Improved Critical (Passive)
        CreateSkillAsset("Beginner/CriticalEye", "critical_eye", "Critical Eye",
            "Passively increases critical hit chance by {damage}%.",
            SkillType.Passive, DamageType.Physical, "beginner",
            5, 5, 1,
            baseDamage: 2, damagePerLevel: 1,
            manaCost: 0, manaCostPerLevel: 0,
            cooldown: 0, cooldownReduction: 0,
            tier: 1, nodePos: new Vector2(1, 1));
    }

    private void CreateWarriorSkills()
    {
        // Slash combo
        CreateSkillAsset("Warrior/TripleSlash", "triple_slash", "Triple Slash",
            "Perform three rapid slashes dealing {damage} total damage.",
            SkillType.Active, DamageType.Physical, "warrior",
            10, 20, 1,
            baseDamage: 45, damagePerLevel: 12,
            manaCost: 15, manaCostPerLevel: 2,
            cooldown: 5f, cooldownReduction: 0.1f,
            tier: 0, nodePos: new Vector2(0, 0));

        // War Cry buff
        CreateSkillAsset("Warrior/WarCry", "war_cry", "War Cry",
            "Increase attack power by 20% for {duration}s.",
            SkillType.Buff, DamageType.Physical, "warrior",
            10, 15, 1,
            baseDamage: 0, damagePerLevel: 0,
            manaCost: 20, manaCostPerLevel: 2,
            cooldown: 30f, cooldownReduction: 1f,
            duration: 10f, durationPerLevel: 0.5f,
            tier: 0, nodePos: new Vector2(1, 0));

        // Ground Slam AoE
        CreateSkillAsset("Warrior/GroundSlam", "ground_slam", "Ground Slam",
            "Slam the ground, dealing {damage} damage to all nearby enemies.",
            SkillType.Active, DamageType.Physical, "warrior",
            15, 20, 2,
            baseDamage: 60, damagePerLevel: 15,
            manaCost: 25, manaCostPerLevel: 3,
            cooldown: 8f, cooldownReduction: 0.2f,
            tier: 1, nodePos: new Vector2(0, 1));

        // Iron Skin passive
        CreateSkillAsset("Warrior/IronSkin", "iron_skin", "Iron Skin",
            "Passively reduces all damage taken by {damage}%.",
            SkillType.Passive, DamageType.Physical, "warrior",
            12, 10, 2,
            baseDamage: 3, damagePerLevel: 1,
            manaCost: 0, manaCostPerLevel: 0,
            cooldown: 0, cooldownReduction: 0,
            tier: 1, nodePos: new Vector2(1, 1));

        // Berserk ultimate
        CreateSkillAsset("Warrior/Berserk", "berserk", "Berserk",
            "Enter a berserk state, increasing damage by 50% but taking 20% more damage for {duration}s.",
            SkillType.Buff, DamageType.Physical, "warrior",
            20, 20, 3,
            baseDamage: 0, damagePerLevel: 0,
            manaCost: 40, manaCostPerLevel: 3,
            cooldown: 60f, cooldownReduction: 2f,
            duration: 15f, durationPerLevel: 1f,
            tier: 2, nodePos: new Vector2(0.5f, 2));
    }

    private void CreateMageSkills()
    {
        // Fireball
        CreateSkillAsset("Mage/Fireball", "fireball", "Fireball",
            "Launch a fireball dealing {damage} fire damage.",
            SkillType.Active, DamageType.Fire, "mage",
            10, 20, 1,
            baseDamage: 40, damagePerLevel: 10,
            manaCost: 12, manaCostPerLevel: 2,
            cooldown: 4f, cooldownReduction: 0.1f,
            tier: 0, nodePos: new Vector2(0, 0));

        // Ice Bolt
        CreateSkillAsset("Mage/IceBolt", "ice_bolt", "Ice Bolt",
            "Fire an ice bolt dealing {damage} ice damage and slowing the target.",
            SkillType.Active, DamageType.Ice, "mage",
            10, 20, 1,
            baseDamage: 30, damagePerLevel: 8,
            manaCost: 10, manaCostPerLevel: 1,
            cooldown: 3f, cooldownReduction: 0.1f,
            tier: 0, nodePos: new Vector2(1, 0));

        // Magic Shield
        CreateSkillAsset("Mage/MagicShield", "magic_shield", "Magic Shield",
            "Create a shield that absorbs {damage} damage for {duration}s.",
            SkillType.Buff, DamageType.Magic, "mage",
            12, 15, 1,
            baseDamage: 50, damagePerLevel: 15,
            manaCost: 25, manaCostPerLevel: 3,
            cooldown: 20f, cooldownReduction: 0.5f,
            duration: 8f, durationPerLevel: 0.3f,
            tier: 1, nodePos: new Vector2(0.5f, 1));

        // Mana Mastery passive
        CreateSkillAsset("Mage/ManaMastery", "mana_mastery", "Mana Mastery",
            "Passively increases max MP by {damage}% and MP regen by {damage}%.",
            SkillType.Passive, DamageType.Magic, "mage",
            15, 10, 2,
            baseDamage: 5, damagePerLevel: 2,
            manaCost: 0, manaCostPerLevel: 0,
            cooldown: 0, cooldownReduction: 0,
            tier: 1, nodePos: new Vector2(1.5f, 1));

        // Meteor ultimate
        CreateSkillAsset("Mage/Meteor", "meteor", "Meteor",
            "Call down a meteor dealing {damage} fire damage in a large area.",
            SkillType.Active, DamageType.Fire, "mage",
            20, 20, 3,
            baseDamage: 150, damagePerLevel: 30,
            manaCost: 60, manaCostPerLevel: 5,
            cooldown: 45f, cooldownReduction: 1.5f,
            castTime: 1.5f,
            tier: 2, nodePos: new Vector2(0.5f, 2));
    }

    private void CreateRogueSkills()
    {
        // Quick Strike
        CreateSkillAsset("Rogue/QuickStrike", "quick_strike", "Quick Strike",
            "A rapid strike dealing {damage} damage with high critical chance.",
            SkillType.Active, DamageType.Physical, "rogue",
            10, 20, 1,
            baseDamage: 25, damagePerLevel: 7,
            manaCost: 8, manaCostPerLevel: 1,
            cooldown: 2f, cooldownReduction: 0.05f,
            tier: 0, nodePos: new Vector2(0, 0));

        // Poison Blade
        CreateSkillAsset("Rogue/PoisonBlade", "poison_blade", "Poison Blade",
            "Coat your blade in poison, dealing {damage} poison damage over 5s.",
            SkillType.Active, DamageType.Poison, "rogue",
            10, 20, 1,
            baseDamage: 35, damagePerLevel: 10,
            manaCost: 15, manaCostPerLevel: 2,
            cooldown: 10f, cooldownReduction: 0.3f,
            duration: 5f, durationPerLevel: 0.2f,
            tier: 0, nodePos: new Vector2(1, 0));

        // Evasion
        CreateSkillAsset("Rogue/Evasion", "evasion", "Evasion",
            "Increase dodge chance by 50% for {duration}s.",
            SkillType.Buff, DamageType.Physical, "rogue",
            12, 15, 1,
            baseDamage: 0, damagePerLevel: 0,
            manaCost: 20, manaCostPerLevel: 2,
            cooldown: 25f, cooldownReduction: 0.5f,
            duration: 5f, durationPerLevel: 0.3f,
            tier: 1, nodePos: new Vector2(0, 1));

        // Critical Mastery passive
        CreateSkillAsset("Rogue/CriticalMastery", "critical_mastery", "Critical Mastery",
            "Passively increases critical damage by {damage}%.",
            SkillType.Passive, DamageType.Physical, "rogue",
            15, 10, 2,
            baseDamage: 10, damagePerLevel: 5,
            manaCost: 0, manaCostPerLevel: 0,
            cooldown: 0, cooldownReduction: 0,
            tier: 1, nodePos: new Vector2(1, 1));

        // Shadow Strike ultimate
        CreateSkillAsset("Rogue/ShadowStrike", "shadow_strike", "Shadow Strike",
            "Vanish and strike from the shadows, dealing {damage} damage with guaranteed critical hit.",
            SkillType.Active, DamageType.Dark, "rogue",
            20, 20, 3,
            baseDamage: 100, damagePerLevel: 25,
            manaCost: 35, manaCostPerLevel: 3,
            cooldown: 30f, cooldownReduction: 1f,
            tier: 2, nodePos: new Vector2(0.5f, 2));
    }

    private SkillData CreateSkillAsset(string path, string id, string name, string desc,
        SkillType type, DamageType damageType, string jobId,
        int reqLevel, int maxLevel, int spCost,
        float baseDamage, float damagePerLevel,
        float manaCost, float manaCostPerLevel,
        float cooldown, float cooldownReduction,
        float duration = 0, float durationPerLevel = 0,
        float castTime = 0,
        int tier = 0, Vector2 nodePos = default)
    {
        string fullPath = $"{SKILLS_PATH}/{path}.asset";
        var skill = AssetDatabase.LoadAssetAtPath<SkillData>(fullPath);

        if (skill == null)
        {
            skill = ScriptableObject.CreateInstance<SkillData>();
            AssetDatabase.CreateAsset(skill, fullPath);
        }

        skill.skillId = id;
        skill.skillName = name;
        skill.description = desc;
        skill.skillType = type;
        skill.damageType = damageType;
        skill.requiredJobId = jobId;
        skill.requiredPlayerLevel = reqLevel;
        skill.maxSkillLevel = maxLevel;
        skill.spCost = spCost;
        skill.baseDamage = baseDamage;
        skill.damagePerLevel = damagePerLevel;
        skill.baseManaCost = manaCost;
        skill.manaCostPerLevel = manaCostPerLevel;
        skill.baseCooldown = cooldown;
        skill.cooldownReductionPerLevel = cooldownReduction;
        skill.baseDuration = duration;
        skill.durationPerLevel = durationPerLevel;
        skill.castTime = castTime;
        skill.tier = tier;
        skill.nodePosition = nodePos;

        EditorUtility.SetDirty(skill);
        return skill;
    }

    #endregion

    #region Skill Trees

    private void CreateSkillTrees()
    {
        EnsureDirectories();

        CreateBeginnerTree();
        CreateWarriorTree();
        CreateMageTree();
        CreateRogueTree();

        AssetDatabase.SaveAssets();
        Debug.Log("[SkillSystemSetup] Skill trees created");
    }

    private void CreateBeginnerTree()
    {
        string path = $"{TREES_PATH}/BeginnerTree.asset";
        var tree = AssetDatabase.LoadAssetAtPath<SkillTreeData>(path);

        if (tree == null)
        {
            tree = ScriptableObject.CreateInstance<SkillTreeData>();
            AssetDatabase.CreateAsset(tree, path);
        }

        tree.treeId = "beginner_tree";
        tree.treeName = "Beginner Skills";
        tree.horizontalSpacing = 150f;
        tree.verticalSpacing = 120f;

        // Load skills
        var powerStrike = AssetDatabase.LoadAssetAtPath<SkillData>($"{SKILLS_PATH}/Beginner/PowerStrike.asset");
        var guard = AssetDatabase.LoadAssetAtPath<SkillData>($"{SKILLS_PATH}/Beginner/Guard.asset");
        var recovery = AssetDatabase.LoadAssetAtPath<SkillData>($"{SKILLS_PATH}/Beginner/Recovery.asset");
        var criticalEye = AssetDatabase.LoadAssetAtPath<SkillData>($"{SKILLS_PATH}/Beginner/CriticalEye.asset");

        tree.nodes = new SkillTreeData.SkillNode[]
        {
            new SkillTreeData.SkillNode { skill = powerStrike, row = 0, column = 0, childNodeIndices = new int[] { 2 } },
            new SkillTreeData.SkillNode { skill = guard, row = 0, column = 1, childNodeIndices = new int[] { 3 } },
            new SkillTreeData.SkillNode { skill = recovery, row = 1, column = 0, childNodeIndices = new int[] { } },
            new SkillTreeData.SkillNode { skill = criticalEye, row = 1, column = 1, childNodeIndices = new int[] { } }
        };

        // Set prerequisites
        if (recovery != null && powerStrike != null)
        {
            recovery.prerequisiteSkills = new SkillData[] { powerStrike };
            recovery.prerequisiteLevels = new int[] { 3 };
            EditorUtility.SetDirty(recovery);
        }

        if (criticalEye != null && guard != null)
        {
            criticalEye.prerequisiteSkills = new SkillData[] { guard };
            criticalEye.prerequisiteLevels = new int[] { 3 };
            EditorUtility.SetDirty(criticalEye);
        }

        tree.connections = new SkillTreeData.NodeConnection[]
        {
            new SkillTreeData.NodeConnection { fromNodeIndex = 0, toNodeIndex = 2 },
            new SkillTreeData.NodeConnection { fromNodeIndex = 1, toNodeIndex = 3 }
        };

        EditorUtility.SetDirty(tree);
    }

    private void CreateWarriorTree()
    {
        string path = $"{TREES_PATH}/WarriorTree.asset";
        var tree = AssetDatabase.LoadAssetAtPath<SkillTreeData>(path);

        if (tree == null)
        {
            tree = ScriptableObject.CreateInstance<SkillTreeData>();
            AssetDatabase.CreateAsset(tree, path);
        }

        tree.treeId = "warrior_tree";
        tree.treeName = "Warrior Skills";
        tree.horizontalSpacing = 150f;
        tree.verticalSpacing = 120f;

        // Load skills
        var tripleSlash = AssetDatabase.LoadAssetAtPath<SkillData>($"{SKILLS_PATH}/Warrior/TripleSlash.asset");
        var warCry = AssetDatabase.LoadAssetAtPath<SkillData>($"{SKILLS_PATH}/Warrior/WarCry.asset");
        var groundSlam = AssetDatabase.LoadAssetAtPath<SkillData>($"{SKILLS_PATH}/Warrior/GroundSlam.asset");
        var ironSkin = AssetDatabase.LoadAssetAtPath<SkillData>($"{SKILLS_PATH}/Warrior/IronSkin.asset");
        var berserk = AssetDatabase.LoadAssetAtPath<SkillData>($"{SKILLS_PATH}/Warrior/Berserk.asset");

        tree.nodes = new SkillTreeData.SkillNode[]
        {
            new SkillTreeData.SkillNode { skill = tripleSlash, row = 0, column = 0, childNodeIndices = new int[] { 2 } },
            new SkillTreeData.SkillNode { skill = warCry, row = 0, column = 1, childNodeIndices = new int[] { 3 } },
            new SkillTreeData.SkillNode { skill = groundSlam, row = 1, column = 0, childNodeIndices = new int[] { 4 } },
            new SkillTreeData.SkillNode { skill = ironSkin, row = 1, column = 1, childNodeIndices = new int[] { 4 } },
            new SkillTreeData.SkillNode { skill = berserk, row = 2, column = 0, childNodeIndices = new int[] { } }
        };

        // Set prerequisites
        if (groundSlam != null && tripleSlash != null)
        {
            groundSlam.prerequisiteSkills = new SkillData[] { tripleSlash };
            groundSlam.prerequisiteLevels = new int[] { 5 };
            EditorUtility.SetDirty(groundSlam);
        }

        if (ironSkin != null && warCry != null)
        {
            ironSkin.prerequisiteSkills = new SkillData[] { warCry };
            ironSkin.prerequisiteLevels = new int[] { 5 };
            EditorUtility.SetDirty(ironSkin);
        }

        if (berserk != null && groundSlam != null && ironSkin != null)
        {
            berserk.prerequisiteSkills = new SkillData[] { groundSlam, ironSkin };
            berserk.prerequisiteLevels = new int[] { 5, 5 };
            EditorUtility.SetDirty(berserk);
        }

        tree.connections = new SkillTreeData.NodeConnection[]
        {
            new SkillTreeData.NodeConnection { fromNodeIndex = 0, toNodeIndex = 2 },
            new SkillTreeData.NodeConnection { fromNodeIndex = 1, toNodeIndex = 3 },
            new SkillTreeData.NodeConnection { fromNodeIndex = 2, toNodeIndex = 4 },
            new SkillTreeData.NodeConnection { fromNodeIndex = 3, toNodeIndex = 4 }
        };

        EditorUtility.SetDirty(tree);
    }

    private void CreateMageTree()
    {
        string path = $"{TREES_PATH}/MageTree.asset";
        var tree = AssetDatabase.LoadAssetAtPath<SkillTreeData>(path);

        if (tree == null)
        {
            tree = ScriptableObject.CreateInstance<SkillTreeData>();
            AssetDatabase.CreateAsset(tree, path);
        }

        tree.treeId = "mage_tree";
        tree.treeName = "Mage Skills";
        tree.horizontalSpacing = 150f;
        tree.verticalSpacing = 120f;

        // Load skills
        var fireball = AssetDatabase.LoadAssetAtPath<SkillData>($"{SKILLS_PATH}/Mage/Fireball.asset");
        var iceBolt = AssetDatabase.LoadAssetAtPath<SkillData>($"{SKILLS_PATH}/Mage/IceBolt.asset");
        var magicShield = AssetDatabase.LoadAssetAtPath<SkillData>($"{SKILLS_PATH}/Mage/MagicShield.asset");
        var manaMastery = AssetDatabase.LoadAssetAtPath<SkillData>($"{SKILLS_PATH}/Mage/ManaMastery.asset");
        var meteor = AssetDatabase.LoadAssetAtPath<SkillData>($"{SKILLS_PATH}/Mage/Meteor.asset");

        tree.nodes = new SkillTreeData.SkillNode[]
        {
            new SkillTreeData.SkillNode { skill = fireball, row = 0, column = 0, childNodeIndices = new int[] { 2, 4 } },
            new SkillTreeData.SkillNode { skill = iceBolt, row = 0, column = 1, childNodeIndices = new int[] { 3 } },
            new SkillTreeData.SkillNode { skill = magicShield, row = 1, column = 0, childNodeIndices = new int[] { 4 } },
            new SkillTreeData.SkillNode { skill = manaMastery, row = 1, column = 1, childNodeIndices = new int[] { } },
            new SkillTreeData.SkillNode { skill = meteor, row = 2, column = 0, childNodeIndices = new int[] { } }
        };

        // Set prerequisites
        if (magicShield != null && fireball != null)
        {
            magicShield.prerequisiteSkills = new SkillData[] { fireball };
            magicShield.prerequisiteLevels = new int[] { 5 };
            EditorUtility.SetDirty(magicShield);
        }

        if (manaMastery != null && iceBolt != null)
        {
            manaMastery.prerequisiteSkills = new SkillData[] { iceBolt };
            manaMastery.prerequisiteLevels = new int[] { 5 };
            EditorUtility.SetDirty(manaMastery);
        }

        if (meteor != null && fireball != null && magicShield != null)
        {
            meteor.prerequisiteSkills = new SkillData[] { fireball, magicShield };
            meteor.prerequisiteLevels = new int[] { 10, 5 };
            EditorUtility.SetDirty(meteor);
        }

        tree.connections = new SkillTreeData.NodeConnection[]
        {
            new SkillTreeData.NodeConnection { fromNodeIndex = 0, toNodeIndex = 2 },
            new SkillTreeData.NodeConnection { fromNodeIndex = 0, toNodeIndex = 4 },
            new SkillTreeData.NodeConnection { fromNodeIndex = 1, toNodeIndex = 3 },
            new SkillTreeData.NodeConnection { fromNodeIndex = 2, toNodeIndex = 4 }
        };

        EditorUtility.SetDirty(tree);
    }

    private void CreateRogueTree()
    {
        string path = $"{TREES_PATH}/RogueTree.asset";
        var tree = AssetDatabase.LoadAssetAtPath<SkillTreeData>(path);

        if (tree == null)
        {
            tree = ScriptableObject.CreateInstance<SkillTreeData>();
            AssetDatabase.CreateAsset(tree, path);
        }

        tree.treeId = "rogue_tree";
        tree.treeName = "Rogue Skills";
        tree.horizontalSpacing = 150f;
        tree.verticalSpacing = 120f;

        // Load skills
        var quickStrike = AssetDatabase.LoadAssetAtPath<SkillData>($"{SKILLS_PATH}/Rogue/QuickStrike.asset");
        var poisonBlade = AssetDatabase.LoadAssetAtPath<SkillData>($"{SKILLS_PATH}/Rogue/PoisonBlade.asset");
        var evasion = AssetDatabase.LoadAssetAtPath<SkillData>($"{SKILLS_PATH}/Rogue/Evasion.asset");
        var criticalMastery = AssetDatabase.LoadAssetAtPath<SkillData>($"{SKILLS_PATH}/Rogue/CriticalMastery.asset");
        var shadowStrike = AssetDatabase.LoadAssetAtPath<SkillData>($"{SKILLS_PATH}/Rogue/ShadowStrike.asset");

        tree.nodes = new SkillTreeData.SkillNode[]
        {
            new SkillTreeData.SkillNode { skill = quickStrike, row = 0, column = 0, childNodeIndices = new int[] { 2 } },
            new SkillTreeData.SkillNode { skill = poisonBlade, row = 0, column = 1, childNodeIndices = new int[] { 3 } },
            new SkillTreeData.SkillNode { skill = evasion, row = 1, column = 0, childNodeIndices = new int[] { 4 } },
            new SkillTreeData.SkillNode { skill = criticalMastery, row = 1, column = 1, childNodeIndices = new int[] { 4 } },
            new SkillTreeData.SkillNode { skill = shadowStrike, row = 2, column = 0, childNodeIndices = new int[] { } }
        };

        // Set prerequisites
        if (evasion != null && quickStrike != null)
        {
            evasion.prerequisiteSkills = new SkillData[] { quickStrike };
            evasion.prerequisiteLevels = new int[] { 5 };
            EditorUtility.SetDirty(evasion);
        }

        if (criticalMastery != null && poisonBlade != null)
        {
            criticalMastery.prerequisiteSkills = new SkillData[] { poisonBlade };
            criticalMastery.prerequisiteLevels = new int[] { 5 };
            EditorUtility.SetDirty(criticalMastery);
        }

        if (shadowStrike != null && evasion != null && criticalMastery != null)
        {
            shadowStrike.prerequisiteSkills = new SkillData[] { evasion, criticalMastery };
            shadowStrike.prerequisiteLevels = new int[] { 5, 5 };
            EditorUtility.SetDirty(shadowStrike);
        }

        tree.connections = new SkillTreeData.NodeConnection[]
        {
            new SkillTreeData.NodeConnection { fromNodeIndex = 0, toNodeIndex = 2 },
            new SkillTreeData.NodeConnection { fromNodeIndex = 1, toNodeIndex = 3 },
            new SkillTreeData.NodeConnection { fromNodeIndex = 2, toNodeIndex = 4 },
            new SkillTreeData.NodeConnection { fromNodeIndex = 3, toNodeIndex = 4 }
        };

        EditorUtility.SetDirty(tree);
    }

    #endregion

    #region Linking

    private void LinkJobsToTrees()
    {
        // Link jobs to their skill trees
        var beginner = AssetDatabase.LoadAssetAtPath<JobClassData>($"{JOBS_PATH}/Beginner.asset");
        var warrior = AssetDatabase.LoadAssetAtPath<JobClassData>($"{JOBS_PATH}/Warrior.asset");
        var mage = AssetDatabase.LoadAssetAtPath<JobClassData>($"{JOBS_PATH}/Mage.asset");
        var rogue = AssetDatabase.LoadAssetAtPath<JobClassData>($"{JOBS_PATH}/Rogue.asset");

        var beginnerTree = AssetDatabase.LoadAssetAtPath<SkillTreeData>($"{TREES_PATH}/BeginnerTree.asset");
        var warriorTree = AssetDatabase.LoadAssetAtPath<SkillTreeData>($"{TREES_PATH}/WarriorTree.asset");
        var mageTree = AssetDatabase.LoadAssetAtPath<SkillTreeData>($"{TREES_PATH}/MageTree.asset");
        var rogueTree = AssetDatabase.LoadAssetAtPath<SkillTreeData>($"{TREES_PATH}/RogueTree.asset");

        if (beginner != null && beginnerTree != null)
        {
            beginner.skillTree = beginnerTree;
            beginner.availableSkills = beginnerTree.GetAllSkills();
            EditorUtility.SetDirty(beginner);
        }

        if (warrior != null && warriorTree != null)
        {
            warrior.skillTree = warriorTree;
            warrior.availableSkills = warriorTree.GetAllSkills();
            EditorUtility.SetDirty(warrior);
        }

        if (mage != null && mageTree != null)
        {
            mage.skillTree = mageTree;
            mage.availableSkills = mageTree.GetAllSkills();
            EditorUtility.SetDirty(mage);
        }

        if (rogue != null && rogueTree != null)
        {
            rogue.skillTree = rogueTree;
            rogue.availableSkills = rogueTree.GetAllSkills();
            EditorUtility.SetDirty(rogue);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[SkillSystemSetup] Jobs linked to skill trees");
    }

    #endregion

    #region SkillManager Configuration

    private void ConfigureSkillManager()
    {
        // Find or create SkillManager in scene
        var skillManager = FindAnyObjectByType<SkillManager>();

        if (skillManager == null)
        {
            // Try to find Managers GameObject
            var managersGO = GameObject.Find("Managers");
            if (managersGO == null)
            {
                managersGO = new GameObject("Managers");
            }

            skillManager = managersGO.AddComponent<SkillManager>();
            Debug.Log("[SkillSystemSetup] Created SkillManager on Managers GameObject");
        }

        // Use SerializedObject to set the private serialized fields
        var so = new SerializedObject(skillManager);

        // Set default job
        var defaultJobProp = so.FindProperty("defaultJob");
        var beginnerJob = AssetDatabase.LoadAssetAtPath<JobClassData>($"{JOBS_PATH}/Beginner.asset");
        if (defaultJobProp != null && beginnerJob != null)
        {
            defaultJobProp.objectReferenceValue = beginnerJob;
        }

        // Collect all skills
        var allSkills = new System.Collections.Generic.List<SkillData>();
        string[] skillGuids = AssetDatabase.FindAssets("t:SkillData", new[] { SKILLS_PATH });
        foreach (var guid in skillGuids)
        {
            string skillPath = AssetDatabase.GUIDToAssetPath(guid);
            var skill = AssetDatabase.LoadAssetAtPath<SkillData>(skillPath);
            if (skill != null)
            {
                allSkills.Add(skill);
            }
        }

        var allSkillDataProp = so.FindProperty("allSkillData");
        if (allSkillDataProp != null)
        {
            allSkillDataProp.arraySize = allSkills.Count;
            for (int i = 0; i < allSkills.Count; i++)
            {
                allSkillDataProp.GetArrayElementAtIndex(i).objectReferenceValue = allSkills[i];
            }
        }

        // Collect all jobs
        var allJobs = new System.Collections.Generic.List<JobClassData>();
        string[] jobGuids = AssetDatabase.FindAssets("t:JobClassData", new[] { JOBS_PATH });
        foreach (var guid in jobGuids)
        {
            string jobPath = AssetDatabase.GUIDToAssetPath(guid);
            var job = AssetDatabase.LoadAssetAtPath<JobClassData>(jobPath);
            if (job != null)
            {
                allJobs.Add(job);
            }
        }

        var allJobDataProp = so.FindProperty("allJobData");
        if (allJobDataProp != null)
        {
            allJobDataProp.arraySize = allJobs.Count;
            for (int i = 0; i < allJobs.Count; i++)
            {
                allJobDataProp.GetArrayElementAtIndex(i).objectReferenceValue = allJobs[i];
            }
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(skillManager);

        Debug.Log($"[SkillSystemSetup] SkillManager configured with {allSkills.Count} skills and {allJobs.Count} jobs");
    }

    #endregion
}
#endif
