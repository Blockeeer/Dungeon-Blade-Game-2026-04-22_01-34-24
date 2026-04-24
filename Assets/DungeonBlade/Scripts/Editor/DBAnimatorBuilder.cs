#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace DungeonBlade.EditorTools
{
    /// <summary>
    /// Builds Animator Controllers with every parameter/trigger the gameplay
    /// code expects. Drop real motion clips onto these states later; wiring
    /// stays intact.
    /// </summary>
    public static class DBAnimatorBuilder
    {
        public static void BuildAll()
        {
            BuildPlayerAnimator();
            BuildEnemyAnimator();
            BuildBossAnimator();
        }

        static AnimatorController BuildPlayerAnimator()
        {
            var ac = Create("Player.controller");
            AddFloat(ac, "Speed");
            AddInt(ac, "WeaponMode");
            AddInt(ac, "ComboStep");
            AddBool(ac, "Blocking");
            AddBool(ac, "ADS");
            AddBool(ac, "Airborne");
            AddBool(ac, "Dashing");
            foreach (var t in new[] { "LightSlash", "HeavySlash", "Fire", "Reload", "Jump" }) AddTrigger(ac, t);

            var sm = ac.layers[0].stateMachine;
            var idle       = sm.AddState("Idle");
            var locomotion = sm.AddState("Locomotion");
            var jump       = sm.AddState("Jump");
            var slash      = sm.AddState("SwordSlash");
            var heavy      = sm.AddState("SwordHeavy");
            var block      = sm.AddState("Blocking");
            var fire       = sm.AddState("Fire");
            var reload     = sm.AddState("Reload");
            var dash       = sm.AddState("Dash");
            sm.defaultState = idle;

            Float(idle, locomotion, "Speed", AnimatorConditionMode.Greater, 0.1f);
            Float(locomotion, idle, "Speed", AnimatorConditionMode.Less, 0.1f);
            AnyTrig(sm, slash,  "LightSlash");
            AnyTrig(sm, heavy,  "HeavySlash");
            AnyTrig(sm, fire,   "Fire");
            AnyTrig(sm, reload, "Reload");
            AnyTrig(sm, jump,   "Jump");
            Exit(slash, idle, 0.4f);
            Exit(heavy, idle, 0.6f);
            Exit(fire, idle, 0.15f);
            Exit(reload, idle, 1.4f);
            Exit(jump, idle, 0.8f);
            Bool(idle, block, "Blocking", true);
            Bool(block, idle, "Blocking", false);
            Bool(idle, dash, "Dashing", true);
            Bool(dash, idle, "Dashing", false);

            AssetDatabase.SaveAssets();
            return ac;
        }

        static AnimatorController BuildEnemyAnimator()
        {
            var ac = Create("Enemy.controller");
            AddFloat(ac, "Speed");
            AddInt(ac, "State");
            foreach (var t in new[] { "Attack", "Stagger", "Death", "Slam", "ShieldBash" }) AddTrigger(ac, t);

            var sm = ac.layers[0].stateMachine;
            var idle    = sm.AddState("Idle");
            var run     = sm.AddState("Run");
            var attack  = sm.AddState("Attack");
            var stagger = sm.AddState("Stagger");
            var death   = sm.AddState("Death");
            sm.defaultState = idle;

            Float(idle, run, "Speed", AnimatorConditionMode.Greater, 0.1f);
            Float(run, idle, "Speed", AnimatorConditionMode.Less, 0.1f);
            AnyTrig(sm, attack,  "Attack");
            AnyTrig(sm, stagger, "Stagger");
            AnyTrig(sm, death,   "Death");
            Exit(attack, idle, 0.7f);
            Exit(stagger, idle, 0.5f);

            AssetDatabase.SaveAssets();
            return ac;
        }

        static AnimatorController BuildBossAnimator()
        {
            var ac = Create("Boss_UndeadWarlord.controller");
            AddFloat(ac, "Speed");
            AddInt(ac, "State"); AddInt(ac, "ComboStep");
            AddBool(ac, "Enraged");
            foreach (var t in new[] { "Attack", "Stagger", "Death", "Combo", "Stomp", "Shockwave", "Summon", "BoneThrow", "PhaseTransition" })
                AddTrigger(ac, t);

            var sm = ac.layers[0].stateMachine;
            var idle    = sm.AddState("Idle");
            var run     = sm.AddState("Run");
            var combo   = sm.AddState("Combo");
            var stomp   = sm.AddState("Stomp");
            var shock   = sm.AddState("Shockwave");
            var summon  = sm.AddState("Summon");
            var bone    = sm.AddState("BoneThrow");
            var death   = sm.AddState("Death");
            var phase   = sm.AddState("PhaseTransition");
            sm.defaultState = idle;

            Float(idle, run, "Speed", AnimatorConditionMode.Greater, 0.1f);
            Float(run, idle, "Speed", AnimatorConditionMode.Less, 0.1f);
            AnyTrig(sm, combo,  "Combo");
            AnyTrig(sm, stomp,  "Stomp");
            AnyTrig(sm, shock,  "Shockwave");
            AnyTrig(sm, summon, "Summon");
            AnyTrig(sm, bone,   "BoneThrow");
            AnyTrig(sm, death,  "Death");
            AnyTrig(sm, phase,  "PhaseTransition");
            Exit(combo, idle, 1.4f);
            Exit(stomp, idle, 1.0f);
            Exit(shock, idle, 1.0f);
            Exit(summon, idle, 0.8f);
            Exit(bone, idle, 0.7f);
            Exit(phase, idle, 1.2f);

            AssetDatabase.SaveAssets();
            return ac;
        }

        // ───── helpers ─────
        static AnimatorController Create(string fileName)
        {
            string path = DBEditorMenu.AnimatorsPath + "/" + fileName;
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
                AssetDatabase.DeleteAsset(path);
            return AnimatorController.CreateAnimatorControllerAtPath(path);
        }
        static void AddFloat(AnimatorController ac, string name)   => ac.AddParameter(name, AnimatorControllerParameterType.Float);
        static void AddInt(AnimatorController ac, string name)     => ac.AddParameter(name, AnimatorControllerParameterType.Int);
        static void AddBool(AnimatorController ac, string name)    => ac.AddParameter(name, AnimatorControllerParameterType.Bool);
        static void AddTrigger(AnimatorController ac, string name) => ac.AddParameter(name, AnimatorControllerParameterType.Trigger);

        static void Float(AnimatorState from, AnimatorState to, string param, AnimatorConditionMode mode, float threshold)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = 0.1f;
            t.AddCondition(mode, threshold, param);
        }
        static void Bool(AnimatorState from, AnimatorState to, string param, bool value)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = 0.1f;
            t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, param);
        }
        static void AnyTrig(AnimatorStateMachine sm, AnimatorState to, string trigger)
        {
            var t = sm.AddAnyStateTransition(to);
            t.hasExitTime = false;
            t.duration = 0.05f;
            t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }
        static void Exit(AnimatorState from, AnimatorState to, float exitTime)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = true;
            t.exitTime = exitTime;
            t.duration = 0.08f;
        }
    }
}
#endif
