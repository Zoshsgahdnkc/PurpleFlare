using AlienRace;
using PurpleFlare.FlairCapabilities;
using RimWorld;
using UnityEngine;
using Verse;
using Wolfein;

namespace PurpleFlare;

public class SpecialCharacterGenerateUtils
{
    private static void ForceGenerateApparels(Pawn pawn, List<ThingDef> thingDefs, Func<QualityCategory> qualityGetter, ThingDef stuff)
    {
        pawn.apparel.DestroyAll();
        foreach (var thingDef in thingDefs)
        {
            Apparel? apparel;
            if (thingDef.MadeFromStuff)
            {
                apparel = ThingMaker.MakeThing(thingDef, stuff) as Apparel;
            }
            else
            {
                apparel = ThingMaker.MakeThing(thingDef) as Apparel;
            }
            if (apparel == null)
            {
                FlairUtils.LogError($"Apparel {thingDef.defName} could not be created or parsed!");
                return;
            }
            var quality = qualityGetter();
            apparel.compQuality.SetQuality(quality, quality >= QualityCategory.Masterwork ? ArtGenerationContext.Colony : null);
            pawn.apparel.Wear(apparel);
        }
    }
    
    internal static Pawn FLAIR()
    {
        FlairUtils.LogDebug("Generating Flair...");
        
        var kind = FlairDefOf.Flair_Flair;
        var PGR = new PawnGenerationRequest(
            kind,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false,
            allowAddictions: false,
            fixedGender:Gender.Female,
            forcedXenotype: FlairDefOf.Wolfein_Xenotype,
            forceRecruitable: true
            );
        
        var Flair = PawnGenerator.GeneratePawn(PGR);
        if (Flair == null)
        {
            FlairUtils.LogError("Failed to generate Flair!");
            Flair = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind));
        }

        ForceGenerateApparels(Flair, kind.apparelRequired, () => kind.itemQuality, FlairUtils.Thing_Wolfein_Fabric());
        
        // 调整HAR部件
        try
        {
            var comp = Flair.TryGetComp<AlienPartGenerator.AlienComp>();
            if (comp != null)
            {
                comp.headVariant = 3;
                comp.OverwriteColorChannel("skin", second: new Color32(255, 255, 255, 255));
                // 0尾巴、1右耳、2左耳、3呆毛、4眉毛、5头发、6左腿、7右腿，FA移除了5
                List<int> addons = [0, 2, 2, 1, 2, 13, 0, 0];
                comp.addonVariants = addons;
            }
        }
        catch (Exception e)
        {
            FlairUtils.LogError(e.StackTrace);
        }
        
        Flair.SetFaction(Faction.OfPlayer);
        FlairCapsComponent.Get().updateFlairHediffs(Flair);
        
        return Flair;
    }
}