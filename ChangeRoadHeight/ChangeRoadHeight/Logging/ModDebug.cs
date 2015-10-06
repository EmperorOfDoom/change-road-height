using UnityEngine;

namespace ChangeRoadHeight
{
    public static class ModDebug {
        const string prefix = "ChangeRoadHeight: ";
        static bool debuggingEnabled = false;

        public static void Log(object s) {
            if (!debuggingEnabled) return;
            Debug.Log(addSuffix(ColossalFramework.Plugins.PluginManager.MessageType.Message, s));
        }

        public static void Error(object s) {
            if (!debuggingEnabled) return;
            Debug.Log(addSuffix(ColossalFramework.Plugins.PluginManager.MessageType.Error, s));
        }

        public static void Warning(object s) {
            if (!debuggingEnabled) return;
            Debug.Log(addSuffix(ColossalFramework.Plugins.PluginManager.MessageType.Warning, s));
        }

        static string ObjectToString(object s) {
            return s != null ? s.ToString() : "(null)";
        }

        private static string addSuffix(ColossalFramework.Plugins.PluginManager.MessageType messageType, object s)
        {
            return messageType + " | " + prefix + ObjectToString(s) + " | " + prefix + ObjectToString(s);
        }
    }
}
