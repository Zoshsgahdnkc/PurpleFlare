using RimWorld;
using Verse;

namespace PurpleFlare.FlairCapabilities;

public class FlairCapsComponent: GameComponent
{
    
    public FlairCapsComponent(Game game) {}
    
    public static FlairCapsComponent Get() => Current.Game.GetComponent<FlairCapsComponent>();

    private float _compassion;
    public float Compassion
    {
        get => _compassion;
        set
        {
            if (value > 100 || value < -100)
            {
                FlairUtils.LogError("Compassion value must be between -100 and 100, current value: " + value);
                value = Math.Clamp(value, -100f, 100f);
            }
            _compassion = value;
        }
    }

    private FlairAttributes _attributes = new FlairAttributes();

    /*
     * 属性成长唯一入口：
     * FlairAttributes 只负责数值计算并返回是否升级；
     * Hediff 更新和升级信件属于组件副作用，统一留在 Component 中处理。
     */
    public bool GrowAttribute(Pawn flair, FlairAttributeType type, float value)
    {
        if (!_attributes.GrowAttribute(flair, type, value, out int newLevel)) return false;

        updateFlairHediffs(flair);
        SendAttributeLevelUpLetter(flair, type, newLevel);
        return true;
    }

    /*
     * 从原 FlairAttributesManager.TriggerLevelUp 迁移过来的升级副作用逻辑。
     * 使用调用方传入的 flair，避免属性对象内部再次调用
     * SpecialCharacterComponent.Get().GetOrCreateFlair()，从而解除反向依赖。
     */
    private void SendAttributeLevelUpLetter(Pawn flair, FlairAttributeType type, int level)
    {
        TaggedString attributeString = _attributes.GetTranslateString(type);
        TaggedString label = "purpleflare.letter_label.attribute_level_up".Translate(attributeString);
        TaggedString text = "purpleflare.letter_text.attribute_level_up".Translate(
            attributeString.Named("ATTRIBUTE"),
            level.Named("LEVEL"),
            flair.Named("FLAIR"));

        Find.LetterStack.ReceiveLetter(LetterMaker.MakeLetter(label, text, LetterDefOf.PositiveEvent, flair));
    }

    public int GetCurrentLevel(FlairAttributeType type) => _attributes.GetCurrentLevel(type);

    public float GetAmountInCurrentLevel(FlairAttributeType type) => _attributes.GetAmountInCurrentLevel(type);

    public float ResolveLevelToAmount(int level) => _attributes.ResolveLevelToAmount(level);

    public TaggedString GetAttributeTranslateString(FlairAttributeType type) => _attributes.GetTranslateString(type);

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _compassion, "compassion");
        Scribe_Deep.Look(ref _attributes, "attributes");
        if (Scribe.mode == LoadSaveMode.PostLoadInit) nextTick = Find.TickManager.TicksGame + TickInterval;
    }
    
    # region Ticking Fields and Methods

    // 10秒更新一次
    // 使用赋值最小化开销
    public const int TickInterval = 200;
    private int nextTick;

    public override void FinalizeInit()
    {
        int now = Find.TickManager.TicksGame;
        nextTick = now + TickInterval;
        base.FinalizeInit();
    }

    public override void GameComponentTick()
    {
        base.GameComponentTick();
        // 10秒之间的过滤掉
        int now = Find.TickManager.TicksGame;
        if (now < nextTick) return;
        nextTick = now + TickInterval;
        IntervalTick();
    }

    private void IntervalTick()
    {
        var comp = SpecialCharacterComponent.Get();
        if (!comp.IsFlairGenerated()) return;
        updateFlairHediffs(comp.GetOrCreateFlair());
    }

    public void updateFlairHediffs(Pawn flair)
    {
        /*
         * stage 和 severity 的对应关系：0~20~40~60~80~100。
         */
        flair.health.GetOrAddHediff(FlairDefOf.Flair_AttributeHediff_Combat).Severity = _attributes.GetCurrentLevel(FlairAttributeType.Combat) * 0.2f - 0.1f;
        flair.health.GetOrAddHediff(FlairDefOf.Flair_AttributeHediff_Politics).Severity = _attributes.GetCurrentLevel(FlairAttributeType.Politics) * 0.2f - 0.1f;
        flair.health.GetOrAddHediff(FlairDefOf.Flair_AttributeHediff_Negotiation).Severity = _attributes.GetCurrentLevel(FlairAttributeType.Negotiation) * 0.2f - 0.1f;
        flair.health.GetOrAddHediff(FlairDefOf.Flair_AttributeHediff_Knowledge).Severity = _attributes.GetCurrentLevel(FlairAttributeType.Knowledge) * 0.2f - 0.1f;
    }

    # endregion
}
