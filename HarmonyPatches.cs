using HarmonyLib;
using PurpleFlare.FlairCapabilities;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace PurpleFlare;

public class HarmonyPatches
{
    [StaticConstructorOnStartup]
    public class StartUp
    {
        static StartUp()
        {
            var harmony = new Harmony("com.zoshsgahdnkc.purpleflare");
            harmony.PatchAll();
            Log.Message("[PurpleFlare]patch complete");
        }

        // 近战时提升属性
        [HarmonyPatch(typeof (Verb_MeleeAttack), "TryCastShot")]
        public static class Patch_Verb_MeleeAttack_TryCastShot
        {
            private const float BASE_VALUE = 70f;
            [HarmonyPostfix]
            public static void Postfix(ref Verb_MeleeAttack __instance)
            {
                // 只有命中才会计算。有点太苛刻了，故取消。
                // if (!__result) return;
                if (__instance.CasterIsPawn && !SpecialCharacterComponent.Get().isFlair(__instance.CasterPawn)) return;

                float skillMultiplier = __instance.CasterPawn.skills == null
                    ? 1f
                    : 1 + (__instance.CasterPawn.skills.GetSkill(SkillDefOf.Melee).GetLevel() / 20f);
                float exp = BASE_VALUE * __instance.verbProps.AdjustedFullCycleTime(__instance, __instance.CasterPawn) * skillMultiplier;
                
                FlairCapsComponent.Get().GrowAttribute(__instance.CasterPawn, FlairAttributeType.Combat, exp);
            }
        }
        
        // 远程时提升属性
        [HarmonyPatch(typeof (Verb_Shoot), "WarmupComplete")]
        public static class Patch_Verb_Shoot_WarmupComplete
        {
            private const float BASE_VALUE = 40f;
            private const float BASE_VALUE_AimingNeutral = 5f;
            [HarmonyPostfix]
            public static void Postfix(ref Verb_Shoot __instance)
            {
                if (!(__instance.CurrentTarget.Thing is Pawn thing) || thing.Downed || thing.IsColonyMech || !__instance.CasterIsPawn)
                    return;
                if (!SpecialCharacterComponent.Get().isFlair(__instance.CasterPawn)) return;

                float skillMultiplier = __instance.CasterPawn.skills == null
                    ? 1f
                    : 1 + (__instance.CasterPawn.skills.GetSkill(SkillDefOf.Shooting).GetLevel() / 20f);
                float num1 = thing.HostileTo(__instance.caster) ? BASE_VALUE : BASE_VALUE_AimingNeutral;
                float exp = num1 * __instance.verbProps.AdjustedFullCycleTime(__instance, __instance.CasterPawn) * skillMultiplier;

                FlairCapsComponent.Get().GrowAttribute(__instance.CasterPawn, FlairAttributeType.Combat, exp);
            }
        }
        
        // 交易时提升属性
        [HarmonyPatch(typeof (Faction), "Notify_PlayerTraded")]
        public static class Patch_Faction_Notify_PlayerTraded
        {
            private const float BASE_VALUE = 1f;
            [HarmonyPostfix]
            public static void Postfix(float marketValueSentByPlayer, Pawn playerNegotiator)
            {
                if (!SpecialCharacterComponent.Get().isFlair(playerNegotiator)) return;
                
                float skillMultiplier = playerNegotiator.skills == null
                    ? 1f
                    : 1 + (playerNegotiator.skills.GetSkill(SkillDefOf.Social).GetLevel() / 20f);
                float exp = BASE_VALUE * marketValueSentByPlayer * skillMultiplier;
                
                FlairCapsComponent.Get().GrowAttribute(playerNegotiator, FlairAttributeType.Negotiation, exp);
            }
        }
        
        // 演讲时提升属性
        [HarmonyPatch(typeof(JobDriver_GiveSpeech), "<MakeNewToils>b__11_3")]
        public static class Patch_JobDriver_GiveSpeech_Tick
        {
            private const float BASE_VALUE = 0.9f;
            // 一次演讲约4250
            [HarmonyPostfix]
            public static void Postfix(JobDriver_GiveSpeech __instance, int delta)
            {
                if (__instance.pawn == null) return;
                if (!SpecialCharacterComponent.Get().isFlair(__instance.pawn)) return;
        
                // 每 tick 给 Negotiation 属性经验
                float exp = BASE_VALUE * delta;
                FlairCapsComponent.Get().GrowAttribute(__instance.pawn, FlairAttributeType.Negotiation, exp);
            }
        }
        
        // 和平谈话时提升属性
        [HarmonyPatch(typeof(PeaceTalks), nameof(PeaceTalks.Notify_CaravanArrived))]
        public static class Patch_PeaceTalks_NotifyCaravanArrived
        {
            private const float BASE_VALUE = 5000f;
    
            [HarmonyPostfix]
            public static void Postfix(PeaceTalks __instance, Caravan caravan)
            {
                Pawn flair = caravan.PawnsListForReading.Where(pawn => pawn.IsColonist)
                    .First(pawn => SpecialCharacterComponent.Get().isFlair(pawn));
                if (flair == null) return;
                
                float skillMultiplier = flair.skills == null 
                    ? 1f 
                    : 1f + (flair.skills.GetSkill(SkillDefOf.Social).GetLevel() / 20f);
                
                Pawn diplomat = BestCaravanPawnUtility.FindBestDiplomat(caravan);
                if (diplomat == flair) skillMultiplier += 1f;
                
                // 默认5000经验，如果是外交官再加5000。
                float exp = BASE_VALUE * skillMultiplier;
        
                FlairCapsComponent.Get().GrowAttribute(diplomat, FlairAttributeType.Negotiation, exp);
            }
        }
        
        // 制作带有未完成品的物品配方时提升属性
        [HarmonyPatch(typeof(Toils_Recipe), nameof(Toils_Recipe.DoRecipeWork))]
        public static class Patch_Toils_Recipe_DoRecipeWork
        {
            private const float BASE_VALUE = 0.1f;
            [HarmonyPostfix]
            public static void Postfix(Toil __result)
            {
                var original = __result.tickIntervalAction;
                __result.tickIntervalAction = (Action<int>)(delta =>
                {
                    original?.Invoke(delta);
                    var actor = __result.actor;
                    if (actor == null || actor.skills == null) return;
                    if (!SpecialCharacterComponent.Get().isFlair(actor)) return;
                    var recipe = actor.CurJob?.bill?.recipe;
                    if ((recipe?.skillRequirements != null && recipe.skillRequirements.Count > 0) || recipe.workSkill != null)
                    {
                        float exp = BASE_VALUE * delta;
                        FlairCapsComponent.Get().GrowAttribute(actor, FlairAttributeType.Knowledge, exp);
                    }
                });
            }
        }
        
        // 制作没有未完成品的配方时提升属性
        [HarmonyPatch(typeof(Toils_Recipe), nameof(Toils_Recipe.FinishRecipeAndStartStoringProduct))]
        public static class Patch_Toils_Recipe_Finish
        {
            private const float BASE_VALUE = 0.08f;
            [HarmonyPostfix]
            public static void Postfix(Toil __result)
            {
                var originalInit = __result.initAction;
                __result.initAction = (Action)(() => 
                {
                    originalInit?.Invoke();
                    var actor = __result.actor;
                    if (actor == null) return;
                    if (!SpecialCharacterComponent.Get().isFlair(actor)) return;
                    var recipe = actor.CurJob?.bill?.recipe;
                    if ((recipe?.skillRequirements != null && recipe.skillRequirements.Count > 0) || recipe.workSkill != null)
                    {
                        var driver = (JobDriver_DoBill)actor.jobs.curDriver;
                        float exp = (driver.ticksSpentDoingRecipeWork * BASE_VALUE); 
                        FlairCapsComponent.Get().GrowAttribute(actor, FlairAttributeType.Knowledge, exp);
                    }
                });
            }
        }
        
        // 研究台研究时提升属性
        [HarmonyPatch(typeof(ResearchManager), "ResearchPerformed")]
        public static class Patch_ResearchManager_ResearchPerformed
        {
            private const float BASE_VALUE = 0.1f;
    
            [HarmonyPostfix]
            public static void Postfix(ResearchManager __instance, Pawn researcher)
            {
                if (researcher == null) return;
                if (researcher.Dead || researcher.Downed) return;
                if (!researcher.Spawned) return;
                if (!SpecialCharacterComponent.Get().isFlair(researcher)) return;
        
                // 不带有原方法中的progress参数，让属性提升仅和研究时间挂钩。
                FlairCapsComponent.Get().GrowAttribute(researcher, FlairAttributeType.Knowledge, BASE_VALUE);
            }
        }
        
    }
}