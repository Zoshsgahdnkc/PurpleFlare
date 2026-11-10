using RimWorld;
using Verse;

namespace PurpleFlare;

[DefOf]
public static class FlairDefOf
{
    public static PawnKindDef Flair_Flair;
    [MayRequire("ancot.wolfeinracegenepatch")]
    public static XenotypeDef Wolfein_Xenotype;

    public static StatDef Flair_AttributeLearningFactor;
    
    public static HediffDef Flair_AttributeHediff_Combat;
    public static HediffDef Flair_AttributeHediff_Politics;
    public static HediffDef Flair_AttributeHediff_Negotiation;
    public static HediffDef Flair_AttributeHediff_Knowledge;

    static FlairDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof (FlairDefOf));
}