
using Verse;

namespace PurpleFlare;

class FlairUtils
{
    #region Log_and_Debug

    public static void LogMessage(string msg)
    {
        Log.Message("[PurpleFlare] " + msg);
    }
    
    public static void LogDebug(string msg)
    {
        if (false) return;
        Log.Message("[PurpleFlare] " + msg);
    }
    
    public static void LogWarning(string msg)
    {
        Log.Warning("[PurpleFlare] " + msg);
    }
    
    public static void LogError(string msg)
    {
        Log.Error("[PurpleFlare] " + msg);
    }
    
    #endregion
    
    
    #region Thing_Utils

    public static ThingDef Thing_Wolfein_Fabric()
    {
        return ThingDef.Named("Wolfein_CompositeFabric");
    }
    
    #endregion
}
