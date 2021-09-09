using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Reflection;
using UnityEngine.SceneManagement;

namespace HellpointDataminer
{
    /// <summary>
    /// Attribute used to mark a type that needs to be serialized by the Serializer.
    /// Usage is to just put [DM_Serialized] on a base class. Derived classes will inherit it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DM_Serialized : Attribute { }

    /// <summary>
    /// HPDataminer's serializer. Handles Xml serialization and deserialization for HPDataminer's custom types.
    /// </summary>
    public class Serializer
    {
        /// <summary>
        /// HPDataminer.dll AppDomain reference.
        /// </summary>
        public static Assembly DM_Assembly
        {
            get
            {
                if (m_dmAssembly == null)
                {
                    m_dmAssembly = Assembly.GetExecutingAssembly();
                }
                return m_dmAssembly;
            }
        }
        private static Assembly m_dmAssembly;

        /// <summary>
        /// The Assembly-Csharp.dll AppDomain reference.
        /// </summary>
        public static Assembly Game_Assembly
        {
            get
            {
                if (m_gameAssembly == null)
                {
                    // Any game-class would work, I just picked Item.
                    m_gameAssembly = typeof(Items.Item).Assembly;
                }

                return m_gameAssembly;
            }
        }
        private static Assembly m_gameAssembly;

        /// <summary>
        /// List of DM_Type classes (types marked as DM_Serialized).
        /// </summary>
        public static Type[] DMTypes
        {
            get
            {
                if (m_dmTypes == null || m_dmTypes.Length < 1)
                {
                    var list = new List<Type>();

                    // add DM_Serialized types (custom types)
                    foreach (var type in DM_Assembly.GetTypes())
                    {
                        // check if marked as DM_Serialized
                        if (type.GetCustomAttributes(typeof(DM_Serialized), true).Length > 0)
                        {
                            list.Add(type);
                        }
                    }

                    m_dmTypes = list.ToArray();
                }

                return m_dmTypes;
            }
        }
        private static Type[] m_dmTypes;

        private static readonly Dictionary<Type, XmlSerializer> m_xmlCache = new Dictionary<Type, XmlSerializer>();

        /// <summary>
        /// Use this to get and cache an XmlSerializer for the provided Type, this will include all DM_Types as the extraTypes.
        /// </summary>
        /// <param name="type">The root type of the document</param>
        /// <returns>The new (or cached) XmlSerializer</returns>
        public static XmlSerializer GetXmlSerializer(Type type)
        {
            if (!m_xmlCache.ContainsKey(type))
            {
                m_xmlCache.Add(type, new XmlSerializer(type, DMTypes));
            }

            return m_xmlCache[type];
        }

        /// <summary>
        /// Pass a SideLoader class type (eg, DM_Item) and get the corresponding Game class (eg, Item).
        /// </summary>
        /// <param name="_dmType">Eg, typeof(DM_Items.Item)</param>
        /// <param name="logging">If you want to log debug messages.</param>
        public static Type GetGameType(Type _dmType, bool logging = true)
        {
            var name = _dmType.Name.Substring(3, _dmType.FullName.Length - 3);

            Type t = null;
            try
            {
                t = Game_Assembly.GetType(name);
                if (t == null) throw new Exception("Null");
            }
            catch (Exception e)
            {
                if (logging)
                {
                    HPDataminer.Log($"Could not get Game_Assembly Type '{name}'", 1);
                    HPDataminer.Log(e.Message, 1);
                    HPDataminer.Log(e.StackTrace, 1);
                }
            }

            return t;
        }

        /// <summary>
        /// Pass a Game Class type (eg, Item) and get the corresponding SideLoader class (eg, DM_Item).
        /// </summary>
        /// <param name="_gameType">Eg, typeof(Items.Item)</param>
        /// <param name="logging">If you want to log debug messages.</param>
        public static Type GetDMType(Type _gameType, bool logging = true)
        {
            var name = $"HellpointDataminer.DM_{_gameType.FullName}";

            Type t = null;
            try
            {
                t = DM_Assembly.GetType(name);
                if (t == null) throw new Exception("Null");
            }
            catch (Exception e)
            {
                if (logging)
                {
                    HPDataminer.Log($"Could not get DM_Assembly Type '{name}'", 1);
                    HPDataminer.Log(e.Message, 1);
                    HPDataminer.Log(e.StackTrace, 1);
                }
            }

            return t;
        }

        /// <summary>
        /// Get the "best-match" for the provided game class.
        /// Will get the highest-level base class of the provided game class with a matching DM class.
        /// </summary>
        /// <param name="type">The game class you want a match for.</param>
        /// <returns>Best-match DM Type, if any, otherwise null.</returns>
        public static Type GetBestDMType(Type type)
        {
            if (GetDMType(type, false) is Type dmType && !dmType.IsAbstract)
            {
                return dmType;
            }
            else
            {
                if (type.BaseType != null)
                {
                    return GetBestDMType(type.BaseType);
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Save an DM_Type object to xml.
        /// </summary>
        public static void SaveToXml(string dir, string saveName, object obj)
        {
            if (!string.IsNullOrEmpty(dir))
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                dir += "/";
            }

            saveName = ReplaceInvalidChars(saveName);

            string path = dir + saveName + ".xml";
            if (File.Exists(path))
            {
                //Debug.LogWarning("SaveToXml: A file already exists at " + path + "! Deleting...");
                File.Delete(path);
            }

            var xml = GetXmlSerializer(obj.GetType());

            FileStream file = File.Create(path);
            xml.Serialize(file, obj);
            file.Close();
        }

        /// <summary>
        /// Load an DM_Type object from XML.
        /// </summary>
        public static object LoadFromXml(string path)
        {
            if (!File.Exists(path))
            {
                HPDataminer.Log("LoadFromXml :: Trying to load an XML but path doesnt exist: " + path);
                return null;
            }

            // First we have to find out what kind of Type this xml was serialized as.
            string typeName = "";
            using (XmlReader reader = XmlReader.Create(path))
            {
                while (reader.Read()) // just get the first element (root) then break.
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        // the real type might be saved as an attribute
                        if (!string.IsNullOrEmpty(reader.GetAttribute("type")))
                        {
                            typeName = reader.GetAttribute("type");
                        }
                        else
                        {
                            typeName = reader.Name;
                        }
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(typeName) && DM_Assembly.GetType($"HellpointDataminer.{typeName}") is Type type)
            {
                var xml = GetXmlSerializer(type);
                FileStream file = File.OpenRead(path);
                var obj = xml.Deserialize(file);
                file.Close();
                return obj;
            }
            else
            {
                HPDataminer.Log("LoadFromXml Error, could not serialize the Type of document! typeName: " + typeName, 1);
                return null;
            }
        }

        /// <summary>Remove invalid filename characters from a string</summary>
        public static string ReplaceInvalidChars(string s)
        {
            return string.Join("_", s.Split(Path.GetInvalidFileNameChars()));
        }
    }
}
