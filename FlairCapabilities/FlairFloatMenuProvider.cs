using RimWorld;
using Verse;

namespace PurpleFlare.FlairCapabilities;

public class FlairFloatMenuProvider: FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;
    protected override bool CanSelfTarget => true;

    public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context) => true;

    protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
    {
        FlairUtils.LogDebug($"clickedPawn: {clickedPawn}");
        if (!SpecialCharacterComponent.Get().isFlair(clickedPawn)) return null;   // 不是目标 pawn 就返回 null

        return new FloatMenuOption("purpleflare.float_menu.talk_to_flair".Translate(clickedPawn.LabelShort), delegate
        {
            Find.WindowStack.Add(new FlairWindow(clickedPawn));
        });
    }
}