using Verse;

namespace PurpleFlare.FlairCapabilities;

/*
 * 新增的属性数据类。
 * 每个 FlairAttribute 对应一个属性类型，保存：
 * 1. Type：属性类型；
 * 2. Exp：当前总经验值；
 * 3. DailyGainedExp：本游戏日已经获得的经验值。
 *
 * 该类只负责数据与存档，不访问 Component、LetterStack 等外部对象。
 * 必须保留无参构造函数，Scribe_Deep 读取 List 元素时会用反射创建实例。
 */
public class FlairAttribute : IExposable
{
    public FlairAttributeType Type;
    public float Exp;
    public float DailyGainedExp;

    public FlairAttribute()
    {
    }

    public FlairAttribute(FlairAttributeType type)
    {
        Type = type;
    }

    /*
     * 新存档格式：
     * 每个属性对象单独保存 type / exp / dailyGainedExp 三个字段。
     * 与旧版 Dictionary<FlairAttribute, float> 的格式不同，
     * 因此 FlairAttributes 中的 List 使用了新的存档节点名 attributeList。
     */
    public void ExposeData()
    {
        Scribe_Values.Look(ref Type, "type");
        Scribe_Values.Look(ref Exp, "exp");
        Scribe_Values.Look(ref DailyGainedExp, "dailyGainedExp");
    }
}
