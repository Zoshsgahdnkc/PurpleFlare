using Verse;

namespace PurpleFlare;

public class SpecialCharacterComponent: GameComponent
{
    public SpecialCharacterComponent(Game game) {}

    public static SpecialCharacterComponent Get() => Current.Game.GetComponent<SpecialCharacterComponent>();
    
    protected Pawn Flair;
    public void SetFlair(Pawn f)
    {
        Flair = f;
    }

    public bool IsFlairGenerated()
    {
        return Flair != null;
    }

    public Pawn GetOrCreateFlair()
    {
        if (Flair == null)
        {
            SetFlair(SpecialCharacterGenerateUtils.FLAIR()); 
            FlairUtils.LogDebug($"Created Flair: {Flair}");
        }
        return Flair;
    }

    public bool isFlair(Pawn p)
    {
        return GetOrCreateFlair() == p;
    }

    public override void ExposeData()
    {
        Scribe_References.Look(ref Flair, "Flair");
    }

}