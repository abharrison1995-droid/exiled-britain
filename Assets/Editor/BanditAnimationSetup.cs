using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using ExiledAlvaston.Combat;
using ExiledAlvaston.World;

/// <summary>
/// One-off setup: builds Idle/Run/Attack Animator Controllers for the Light Bandit (Player)
/// and Heavy Bandit (TestEnemy) sprite packs and wires them into the open scene.
/// Run via Tools/Exiled Alvaston/Setup Bandit Animations with c.unity open.
/// </summary>
public static class BanditAnimationSetup
{
    private const string AnimFolder = "Assets/Animations";

    [MenuItem("Tools/Exiled Alvaston/Setup Bandit Animations")]
    public static void Run()
    {
        if (!AssetDatabase.IsValidFolder(AnimFolder))
            AssetDatabase.CreateFolder("Assets", "Animations");

        EnsureSpritesImported("Assets/3DModels/Bandits/Sprites/Light Bandit/Idle");
        EnsureSpritesImported("Assets/3DModels/Bandits/Sprites/Light Bandit/Run");
        EnsureSpritesImported("Assets/3DModels/Bandits/Sprites/Light Bandit/Attack");
        EnsureSpritesImported("Assets/3DModels/Bandits/Sprites/Light Bandit/Hurt");
        EnsureSpritesImported("Assets/3DModels/Bandits/Sprites/Heavy Bandit/Idle");
        EnsureSpritesImported("Assets/3DModels/Bandits/Sprites/Heavy Bandit/Run");
        EnsureSpritesImported("Assets/3DModels/Bandits/Sprites/Heavy Bandit/Attack");
        EnsureSpritesImported("Assets/3DModels/Bandits/Sprites/Heavy Bandit/Hurt");
        AssetDatabase.Refresh();

        AnimationClip lightIdle = BuildClip("Assets/3DModels/Bandits/Sprites/Light Bandit/Idle", "LightBandit_Idle_", 4, 8f, true);
        AnimationClip lightRun = BuildClip("Assets/3DModels/Bandits/Sprites/Light Bandit/Run", "LightBandit_Run_", 8, 12f, true);
        AnimationClip lightAttack = BuildClip("Assets/3DModels/Bandits/Sprites/Light Bandit/Attack", "LightBandit_Attack_", 8, 14f, false);
        AnimationClip lightHurt = BuildClip("Assets/3DModels/Bandits/Sprites/Light Bandit/Hurt", "LightBandit_Hurt_", 2, 6f, false);

        AnimationClip heavyIdle = BuildClip("Assets/3DModels/Bandits/Sprites/Heavy Bandit/Idle", "HeavyBandit_Idle_", 4, 8f, true);
        AnimationClip heavyRun = BuildClip("Assets/3DModels/Bandits/Sprites/Heavy Bandit/Run", "HeavyBandit_Run_", 8, 12f, true);
        AnimationClip heavyAttack = BuildClip("Assets/3DModels/Bandits/Sprites/Heavy Bandit/Attack", "HeavyBandit_Attack_", 8, 14f, false);
        AnimationClip heavyHurt = BuildClip("Assets/3DModels/Bandits/Sprites/Heavy Bandit/Hurt", "HeavyBandit_Hurt_", 2, 6f, false);

        AnimatorController controller = BuildController(lightIdle, lightRun, lightAttack, lightHurt);
        AnimatorOverrideController overrideController = BuildOverride(controller, heavyIdle, heavyRun, heavyAttack, heavyHurt);

        GameObject player = GameObject.Find("Player");
        GameObject enemy = GameObject.Find("TestEnemy");

        if (player == null) Debug.LogWarning("BanditAnimationSetup: couldn't find 'Player' in the open scene — make sure c.unity is open.");
        if (enemy == null) Debug.LogWarning("BanditAnimationSetup: couldn't find 'TestEnemy' in the open scene — make sure c.unity is open.");

        Animator playerAnimator = WireCharacter(player, controller);
        Animator enemyAnimator = WireCharacter(enemy, overrideController);

        if (player != null)
        {
            var combat = player.GetComponent<CombatController>();
            if (combat != null)
            {
                Undo.RecordObject(combat, "Wire Player Animator");
                combat.PlayerAnimator = playerAnimator;
            }
        }
        if (enemy != null)
        {
            var ai = enemy.GetComponent<EnemyAI>();
            if (ai != null)
            {
                Undo.RecordObject(ai, "Wire Enemy Animator");
                ai.Animator = enemyAnimator;
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        Debug.Log("Bandit animation setup complete: Idle/Run/Attack/Hurt wired for Player (Light Bandit) and TestEnemy (Heavy Bandit). Save the scene (Ctrl+S) to keep it.");
    }

    private static void EnsureSpritesImported(string folder)
    {
        foreach (string path in Directory.GetFiles(folder, "*.png"))
        {
            string assetPath = path.Replace("\\", "/");
            if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
        }
    }

    private static AnimationClip BuildClip(string folder, string prefix, int frameCount, float frameRate, bool loop)
    {
        var clip = new AnimationClip { frameRate = frameRate };
        var keyframes = new ObjectReferenceKeyframe[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            string path = $"{folder}/{prefix}{i}.png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning($"BanditAnimationSetup: missing sprite at {path}");
            keyframes[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = sprite };
        }

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string clipPath = $"{AnimFolder}/{prefix.TrimEnd('_')}.anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) != null)
            AssetDatabase.DeleteAsset(clipPath);
        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static AnimatorController BuildController(AnimationClip idle, AnimationClip run, AnimationClip attack, AnimationClip hurt)
    {
        string path = $"{AnimFolder}/Bandit_Controller.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
            AssetDatabase.DeleteAsset(path);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("MeleeAttack", AnimatorControllerParameterType.Trigger);
        // Named "Hit" (not "Hurt") to match the trigger CombatController/EnemyAI already call.
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState idleState = sm.AddState("Idle");
        idleState.motion = idle;
        sm.defaultState = idleState;

        AnimatorState runState = sm.AddState("Run");
        runState.motion = run;

        AnimatorState attackState = sm.AddState("Attack");
        attackState.motion = attack;

        AnimatorState hurtState = sm.AddState("Hurt");
        hurtState.motion = hurt;

        AnimatorStateTransition toRun = idleState.AddTransition(runState);
        toRun.hasExitTime = false;
        toRun.duration = 0.1f;
        toRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        AnimatorStateTransition toIdle = runState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.1f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        AnimatorStateTransition toAttack = sm.AddAnyStateTransition(attackState);
        toAttack.hasExitTime = false;
        toAttack.duration = 0.05f;
        toAttack.AddCondition(AnimatorConditionMode.If, 0, "MeleeAttack");

        AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 1f;
        attackToIdle.duration = 0.1f;
        attackToIdle.hasFixedDuration = true;

        AnimatorStateTransition toHurt = sm.AddAnyStateTransition(hurtState);
        toHurt.hasExitTime = false;
        toHurt.duration = 0.03f;
        toHurt.AddCondition(AnimatorConditionMode.If, 0, "Hit");

        AnimatorStateTransition hurtToIdle = hurtState.AddTransition(idleState);
        hurtToIdle.hasExitTime = true;
        hurtToIdle.exitTime = 1f;
        hurtToIdle.duration = 0.1f;
        hurtToIdle.hasFixedDuration = true;

        return controller;
    }

    private static AnimatorOverrideController BuildOverride(AnimatorController baseController, AnimationClip idle, AnimationClip run, AnimationClip attack, AnimationClip hurt)
    {
        string path = $"{AnimFolder}/HeavyBandit_Override.overrideController";
        if (AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path) != null)
            AssetDatabase.DeleteAsset(path);

        var overrideController = new AnimatorOverrideController(baseController);
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);
        for (int i = 0; i < overrides.Count; i++)
        {
            AnimationClip orig = overrides[i].Key;
            if (orig == null) continue;
            if (orig.name.EndsWith("Idle")) overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, idle);
            else if (orig.name.EndsWith("Run")) overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, run);
            else if (orig.name.EndsWith("Attack")) overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, attack);
            else if (orig.name.EndsWith("Hurt")) overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, hurt);
        }
        overrideController.ApplyOverrides(overrides);
        AssetDatabase.CreateAsset(overrideController, path);
        return overrideController;
    }

    private static Animator WireCharacter(GameObject root, RuntimeAnimatorController controller)
    {
        if (root == null) return null;

        Transform visual = root.transform.Find("ActorVisual");
        if (visual == null)
        {
            var go = new GameObject("ActorVisual");
            Undo.RegisterCreatedObjectUndo(go, "Setup Bandit Animations");
            go.transform.SetParent(root.transform, false);
            go.AddComponent<SpriteBillboard>();
            visual = go.transform;
        }

        Transform swing = visual.Find("SwingRoot");
        if (swing == null)
        {
            var go = new GameObject("SwingRoot");
            Undo.RegisterCreatedObjectUndo(go, "Setup Bandit Animations");
            go.transform.SetParent(visual, false);
            swing = go.transform;
        }

        var sr = swing.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = swing.gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;
        }

        var animator = swing.GetComponent<Animator>();
        if (animator == null)
            animator = swing.gameObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        return animator;
    }
}
