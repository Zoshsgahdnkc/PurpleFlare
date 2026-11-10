using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace PurpleFlare.FlairCapabilities;

/*
 * 原 FlairAttributesManager 重命名为 FlairAttributes。
 * 现在它只是一个普通 IExposable 数据对象，由 FlairCapsComponent 私有持有，
 * 不再直接访问 FlairCapsComponent、SpecialCharacterComponent 或 Find.LetterStack。
 *
 * 职责划分：
 * 1. FlairAttributes：保存属性列表、每日重置状态，并计算成长结果；
 * 2. FlairCapsComponent：作为唯一外部入口，负责升级后的 Hediff 更新和信件发送。
 */
public class FlairAttributes : IExposable
{
    public static readonly float Exp1 = 20000f;
    public static readonly float Exp2 = 30000f;
    public static readonly float Exp3 = 40000f;
    public static readonly float Exp4 = 50000f;
    public static readonly float Exp5 = 1f;
    public static readonly float MaxExpOneTime = 20000f;
    public static readonly float DailyExpUpLimit = 15000f;
    public static readonly float ExcessExpMultiplier = 0.2f;
    public static float ExpToMax => Exp1 + Exp2 + Exp3 + Exp4 + Exp5;

    private static readonly FlairAttributeType[] FixedTypes =
    {
        FlairAttributeType.Combat,
        FlairAttributeType.Politics,
        FlairAttributeType.Negotiation,
        FlairAttributeType.Knowledge,
    };

    /*
     * _lookup 只是为了运行时快速查找，不参与存档，加载后由 RebuildLookup 重建。
     */
    private List<FlairAttribute> _attributes = new();
    private Dictionary<FlairAttributeType, FlairAttribute> _lookup = new();

    private int _lastDay = -1;

    public FlairAttributes()
    {
        EnsureAllAttributes();
    }

    /*
     * 保证列表中始终恰好包含固定四个属性，并且顺序固定。
     * 读档后也会调用一次，用于补齐缺失数据并重建运行时查找表。
     */
    private void EnsureAllAttributes()
    {
        _attributes ??= new List<FlairAttribute>();
        var normalized = new List<FlairAttribute>(FixedTypes.Length);

        foreach (FlairAttributeType type in FixedTypes)
        {
            FlairAttribute? existing = _attributes.Find(a => a != null && a.Type == type);
            normalized.Add(existing ?? new FlairAttribute(type));
        }

        _attributes = normalized;
        RebuildLookup();
    }

    /*
     * _lookup 是纯运行时缓存，不参与 ExposeData。
     * 任何会改变 _attributes 内容的操作之后都必须调用本方法。
     */
    private void RebuildLookup()
    {
        _lookup = new Dictionary<FlairAttributeType, FlairAttribute>(FixedTypes.Length);

        foreach (FlairAttribute attribute in _attributes)
        {
            _lookup[attribute.Type] = attribute;
        }
    }

    /*
     * 每日经验的懒重置。
     * Grow、查看 UI、读取等级等所有公开访问都会经过 GetAttribute，
     * 因此不需要额外 Tick，也能在跨天后第一次访问时立即重置。
     *
     * 同一天内只比较一个 int，开销可以忽略。
     */
    private void EnsureDailyReset()
    {
        int today = GenDate.DaysPassed;
        if (_lastDay == today) return;

        foreach (FlairAttribute attribute in _attributes)
        {
            attribute.DailyGainedExp = 0f;
        }

        _lastDay = today;
    }

    /*
     * 所有属性读取的统一入口。
     * 在这里执行每日重置，确保“查看时”也会更新 _lastDay 和每日成长量。
     */
    public FlairAttribute GetAttribute(FlairAttributeType type)
    {
        EnsureDailyReset();
        return _lookup[type];
    }

    public float GetExp(FlairAttributeType type)
    {
        return GetAttribute(type).Exp;
    }

    public float GetDailyGainedExp(FlairAttributeType type)
    {
        return GetAttribute(type).DailyGainedExp;
    }

    /*
     * 原 GrowAttribute 中的 TriggerLevelUp 已移到 FlairCapsComponent。
     * 这里只负责修改经验值，并通过 newLevel 返回升级后的等级。
     *
     * 每日上限规则：
     * 1. 只限制正向成长；
     * 2. 每个属性各自拥有 DailyExpUpLimit；
     * 3. DailyGainedExp 只累计最终实际增加的经验。
     */
    public bool GrowAttribute(Pawn flair, FlairAttributeType type, float value, out int newLevel)
    {
        EnsureDailyReset();

        FlairAttribute attribute = GetAttribute(type);
        int prevLevel = ResolveValueToLevel(attribute.Exp);
        newLevel = prevLevel;

        value *= flair.GetStatValue(FlairDefOf.Flair_AttributeLearningFactor);

        if (value > MaxExpOneTime) value = MaxExpOneTime;

        /*
         * 超出剩余每日额度的部分乘以 ExcessExpMultiplier 后继续生效。
         */
        float expWithinDailyLimit = 0f;

        if (value > 0f)
        {
            float remainingDailyExp = DailyExpUpLimit - attribute.DailyGainedExp;

            if (remainingDailyExp < 0f)
            {
                remainingDailyExp = 0f;
            }

            expWithinDailyLimit = Mathf.Min(value, remainingDailyExp);
            float excessExp = value - expWithinDailyLimit;
            value = expWithinDailyLimit + excessExp * ExcessExpMultiplier;
        }

        /*
         * 如果会升到满级，就只加到满级为止。
         * 放在每日上限和超额折算之后，保证最终写入 Exp 的是实际增加量。
         */
        if (value > 0f && attribute.Exp + value > ExpToMax)
        {
            value = ExpToMax - attribute.Exp;
        }

        /*
         * 负值保护：确保减少经验时不会掉级。
         * 使用当前等级起始经验作为下限。
         */
        if (value < 0f)
        {
            float levelStartExp = GetLevelStartExp(prevLevel);

            if (attribute.Exp + value < levelStartExp)
            {
                value = levelStartExp - attribute.Exp;
            }
        }

        attribute.Exp += value;

        /*
         * 只有正向且实际生效的经验才计入本日成长量。
         *
         * 注意：超额部分经过 ExcessExpMultiplier 折算后获得的经验
         * 不再占用每日额度，所以 DailyGainedExp 只累计原本位于
         * 每日上限以内的那部分经验。
         *
         * 如果本次成长被满级截断，则优先把实际增加量算作额度内经验，
         * 避免 DailyGainedExp 被多记。
         */
        if (value > 0f)
        {
            attribute.DailyGainedExp += Mathf.Min(expWithinDailyLimit, value);
        }

        newLevel = ResolveValueToLevel(attribute.Exp);
        return newLevel > prevLevel;
    }

    public int GetCurrentLevel(FlairAttributeType type)
    {
        return ResolveValueToLevel(GetAttribute(type).Exp);
    }

    public TaggedString GetTranslateString(FlairAttributeType type)
    {
        TaggedString result = type switch
        {
            FlairAttributeType.Combat => "purpleflare.attribute.combat".Translate(),
            FlairAttributeType.Politics => "purpleflare.attribute.politics".Translate(),
            FlairAttributeType.Negotiation => "purpleflare.attribute.negotiation".Translate(),
            FlairAttributeType.Knowledge => "purpleflare.attribute.knowledge".Translate(),

            _ => throw new System.ArgumentOutOfRangeException(nameof(type), type, null),
        };
        return result;
    }

    public float GetAmountInCurrentLevel(FlairAttributeType type)
    {
        var value = GetAttribute(type).Exp;
        if (value >= Exp1) value -= Exp1;
        if (value >= Exp2) value -= Exp2;
        if (value >= Exp3) value -= Exp3;
        if (value >= Exp4) value -= Exp4;
        return value;
    }

    public float ResolveLevelToAmount(int level)
    {
        switch (level)
        {
            case 1: return Exp1;
            case 2: return Exp2;
            case 3: return Exp3;
            case 4: return Exp4;
            case 5: return Exp5;
        }
        return 0f;
    }

    private int ResolveValueToLevel(float value)
    {
        if (value < 0)
        {
            FlairUtils.LogWarning(value + " is less than 0");
            return 0;
        }
        else if (value < Exp1)
            return 1;
        else if (value < Exp1 + Exp2)
            return 2;
        else if (value < Exp1 + Exp2 + Exp3)
            return 3;
        else if (value < Exp1 + Exp2 + Exp3 + Exp4)
            return 4;
        else
            return 5;
    }

    public float GetCurrentProgress(FlairAttributeType type)
    {
        FlairAttribute attribute = GetAttribute(type);
        int level = ResolveValueToLevel(attribute.Exp);

        if (level == 5) return 0f;

        return attribute.Exp - GetLevelStartExp(level);
    }

    /*
     * 返回某一等级开始时的累计经验。
     * 例如等级 1 从 0 开始，等级 2 从 Exp1 开始，以此类推。
     * 该辅助方法同时用于负值保护和等级内进度计算。
     */
    private static float GetLevelStartExp(int level)
    {
        switch (level)
        {
            case 1: return 0f;
            case 2: return Exp1;
            case 3: return Exp1 + Exp2;
            case 4: return Exp1 + Exp2 + Exp3;
            case 5: return Exp1 + Exp2 + Exp3 + Exp4;
            default: return 0f;
        }
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref _attributes, "attributeList", LookMode.Deep);
        Scribe_Values.Look(ref _lastDay, "lastDay", -1);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            EnsureAllAttributes();
            EnsureDailyReset();
        }
    }
}

public enum FlairAttributeType
{
    Combat,
    Politics,
    Negotiation,
    Knowledge,
}
