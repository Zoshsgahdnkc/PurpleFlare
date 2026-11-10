using System.Collections;
using LudeonTK;
using PurpleFlare.FlairCapabilities;
using RimWorld;
using Verse;
using Verse.AI.Group;
using Wolfein;

namespace PurpleFlare;

public static class FlairDebugAction
{
    
    # region SpawnCharacter
    [DebugAction("Wolfein-Flair", "Spawn Special Pawn")]
    private static List<DebugActionNode> GenerateSpecialPawn()
    {
        var flair = new DebugActionNode("Spawn Flair", DebugActionType.ToolMap);
        flair.action = () => Spawn(SpecialCharacterComponent.Get().GetOrCreateFlair());

        return [flair];
    }

    private static void Spawn(Pawn pawn)
    {
        GenSpawn.Spawn((Thing) pawn, UI.MouseCell(), Find.CurrentMap);
        if (pawn.Spawned && pawn.Faction != null && pawn.Faction != Faction.OfPlayer)
        {
            Lord lord = (Lord) null;
            if (pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction).Any<Pawn>((Predicate<Pawn>) (p => p != pawn)))
            {
                Pawn p1 = (Pawn) GenClosest.ClosestThing_Global(pawn.Position, (IEnumerable) pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction), validator: (Predicate<Thing>) (p => p != pawn && ((Pawn) p).GetLord() != null));
                lord = p1 != null ? p1.GetLord() : (Lord) null;
            }
            if (lord == null || !lord.CanAddPawn(pawn))
                lord = LordMaker.MakeNewLord(pawn.Faction, (LordJob) new LordJob_DefendPoint(pawn.Position), Find.CurrentMap);
            if (lord != null && lord.LordJob.CanAutoAddPawns)
                lord.AddPawn(pawn);
        }
        pawn.Rotation = Rot4.South;
    }
    
    # endregion
    
    # region Capability
    
    [DebugAction("Wolfein-Flair", "Grow Flair Attributes")]
    private static List<DebugActionNode> GrowAttributes()
    {
        return new List<DebugActionNode> { GrowNode(10f), GrowNode(100f), GrowNode(1000f), GrowNode(10000f) };
    }

    private static DebugActionNode GrowNode(float amount)
    {
        return new DebugActionNode(amount + " points")
        {
            labelGetter = () => amount + " points",
            childGetter = () =>
            {
                var list = new List<DebugActionNode>();
                var flair = SpecialCharacterComponent.Get().GetOrCreateFlair();

                /*
                 * 重构：属性成长入口统一改为 FlairCapsComponent.Get().GrowAttribute(...)；
                 * 枚举 FlairAttribute 已重命名为 FlairAttributeType。
                 */
                list.Add(new DebugActionNode("Combat", DebugActionType.Action, () => FlairCapsComponent.Get().GrowAttribute(flair, FlairAttributeType.Combat, amount)));
                list.Add(new DebugActionNode("Politics", DebugActionType.Action, () => FlairCapsComponent.Get().GrowAttribute(flair, FlairAttributeType.Politics, amount)));
                list.Add(new DebugActionNode("Negotiation", DebugActionType.Action, () => FlairCapsComponent.Get().GrowAttribute(flair, FlairAttributeType.Negotiation, amount)));
                list.Add(new DebugActionNode("Knowledge", DebugActionType.Action, () => FlairCapsComponent.Get().GrowAttribute(flair, FlairAttributeType.Knowledge, amount)));
                return list;
            }
        };
    }
    
    # endregion
}