using Kitchen;
using Steamworks;
using Steamworks.Data;
using System;
using System.Reflection;

namespace KitchenMoreSpeedrunInfo
{
    public class DependencyTest : GameSystemBase
    {
        PropertyInfo _internalSteamUGC = typeof(SteamUGC).GetProperty("Internal", BindingFlags.NonPublic | BindingFlags.Static);

        Type t_iSteamUGCType;
        MethodInfo m_GetAppDependencies;

        Type t_CallResult;
        Type t_GetAppDependenciesResult;
        FieldInfo _gAppIDsField;

        Type t_CallResult_GetAppDependenciesResult;
        MethodInfo m_CallResult_GetAppDependenciesResult_GetResult;
        MethodInfo m_CallResult_GetAppDependenciesResult_OnCompleted;

        object callResultObj;

        protected override void Initialise()
        {
            Assembly assembly = Assembly.GetAssembly(typeof(SteamUGC));
            Main.LogInfo(assembly);
            Type[] types = assembly.GetTypes();
            Main.LogInfo(types.Length);
            foreach (Type type in types)
            {
                switch (type.Name)
                {
                    case "ISteamUGC":
                        t_iSteamUGCType = type;
                        break;
                    case "CallResult`1":
                        t_CallResult = type;
                        break;
                    case "GetAppDependenciesResult_t":
                        t_GetAppDependenciesResult = type;
                        break;

                }
            }

            m_GetAppDependencies = t_iSteamUGCType.GetMethod("GetAppDependencies",
                BindingFlags.NonPublic | BindingFlags.Instance, null,
                new Type[] { typeof(PublishedFileId) }, null);

            t_CallResult_GetAppDependenciesResult = t_CallResult.MakeGenericType(new Type[] { t_GetAppDependenciesResult });
            m_CallResult_GetAppDependenciesResult_GetResult = t_CallResult_GetAppDependenciesResult.GetMethod(
                "GetResult", BindingFlags.Public | BindingFlags.Instance);
            m_CallResult_GetAppDependenciesResult_OnCompleted = t_CallResult_GetAppDependenciesResult.GetMethod(
                "OnCompleted", BindingFlags.Public | BindingFlags.Instance);

            _gAppIDsField = t_GetAppDependenciesResult.GetField("GAppIDs", BindingFlags.NonPublic | BindingFlags.Instance);


            ulong queryAppId = 2909998068;

            callResultObj = m_GetAppDependencies.Invoke(
                _internalSteamUGC.GetValue(null), new object[] { (PublishedFileId)(queryAppId) });

            m_CallResult_GetAppDependenciesResult_OnCompleted.Invoke(callResultObj, new object[] { delegate ()
            {
                Main.LogInfo("Dependencies for {queryAppId}");

                object result = m_CallResult_GetAppDependenciesResult_GetResult.Invoke(callResultObj, new object[] { });
                AppId[] appIds = (AppId[])_gAppIDsField.GetValue(result);

                for (int i = 0; i < appIds.Length; i++)
                {
                    Main.LogInfo($"{i}: {appIds[i]}");
                }
            } });
        }

        protected override void OnUpdate()
        {
        }
    }
}
