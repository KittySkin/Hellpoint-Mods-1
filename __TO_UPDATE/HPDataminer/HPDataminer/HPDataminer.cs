using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnityEngine;

namespace HellpointDataminer
{
    public class HPDataminer : MelonMod
    {
        public class ModInfo
        {
            public const string NAME = "HPDataminer";
            public const string VERSION = "1.0.0";
            public const string AUTHOR = "Sinai";
            public const string GUID = "com.sinai.hellpoint.dataminer";
        }

        public const string OutputFolder = @"Dataminer";
        public const string ItemsFolder = OutputFolder + @"\Items";

        public override void OnApplicationStart()
        {
            if (!Directory.Exists(OutputFolder))
            {
                Directory.CreateDirectory(OutputFolder);
            }   
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                SerializePrefabs();
            }
        }

        public static void SerializePrefabs()
        {
            var items = Resources.FindObjectsOfTypeAll<Definitions.ItemDefinition>();

            foreach (var item in items)
            {
                var holder = (SerializedObject)Activator.CreateInstance(Serializer.GetBestDMType(item.GetType()));
                holder.Serialize(item);

                // Save file
                string type = item.GetType().Name;
                if (item is Il2CppSystem.Object ilObject)
                {
                    type = ilObject.GetIl2CppType().Name;
                }
                var folder = ItemsFolder + @"\" + type;

                Serializer.SaveToXml(folder, item.name, holder);
            }
        }

        /// <summary>
        /// Log a message with MelonLogger.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="level">0- = Log, 1 = Warning, 2+ = Error.</param>
        public static void Log(string message, int level = 0)
        {
            if (level <= 0)
                MelonLogger.Log(message);
            else if (level == 1)
                MelonLogger.LogWarning(message);
            else if (level >= 2)
                MelonLogger.LogError(message);
        }
    }
}
