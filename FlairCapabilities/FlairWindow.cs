using RimWorld;

namespace PurpleFlare.FlairCapabilities;

using UnityEngine;
using Verse;

public class FlairWindow : Window
{
    private static readonly FlairAttributeType[] DisplayAttributeTypes =
    {
        FlairAttributeType.Combat,
        FlairAttributeType.Politics,
        FlairAttributeType.Negotiation,
        FlairAttributeType.Knowledge,
    };

    private Pawn pawn;
    private Vector2 scroll;

    public FlairWindow(Pawn pawn)
    {
        this.pawn = pawn;
        optionalTitle = pawn?.LabelCap;
        doCloseX = true;
        absorbInputAroundWindow = true;
        closeOnClickedOutside = false;
        onlyOneOfTypeAllowed = true;
        draggable = true;
    }

    public override Vector2 InitialSize => new Vector2(420f, 360f);

    public override void DoWindowContents(Rect inRect)
    {
        Rect rect = inRect.ContractedBy(Window.StandardMargin);
        rect.yMax -= 10f;

        var listing = new Listing_Standard();
        listing.Begin(rect);
        
        var caps = FlairCapsComponent.Get();
        listing.Label("purpleflare.capacity.compassion".Translate() + ": " + caps?.Compassion.ToString("F0"));
        foreach (FlairAttributeType type in DisplayAttributeTypes)
        {
            int lvl = caps?.GetCurrentLevel(type) ?? 0;
            listing.Label($"{caps?.GetAttributeTranslateString(type)}: {caps?.GetAmountInCurrentLevel(type)} / {caps?.ResolveLevelToAmount(lvl):F0}  (Lv.{lvl})");
        }

        listing.End();
    }
}
