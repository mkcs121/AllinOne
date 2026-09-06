using System;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace AllInOne.Logic
{
    public static class Settings
    {
        public static bool writeDebug;
        public static bool deleteDebug;
        public static string GoogleMapsApiKey;
        public static string ReplaceLinksTo;
        public static string language = "english";
        public static string textEditorPath;
        public static string textEditorArgs;
        public static bool searchAssetsFiles;
        public static bool searchLibFiles;
        public static int TaskCount;

        //чтение настроек
        public static void Load()
        {
            string settingsPath = Path.Combine(new FileInfo(Process.GetCurrentProcess().MainModule.FileName).DirectoryName, "settings.xml");
            if (!File.Exists(settingsPath))
            {
                language = "english";
                return;
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(settingsPath);
            XmlNode root = doc.SelectSingleNode("root");
            XmlNode settings = root?.SelectSingleNode("settings");
            if (settings == null) return;

            foreach (XmlNode settingsItem in settings)
            {
                if (settingsItem.NodeType == XmlNodeType.Comment || settingsItem.Attributes == null || settingsItem.Attributes.Count == 0) 
                    continue;

                switch (settingsItem.Attributes[0].Value)
                {
                    case "GoogleMapsApiKey":
                        GoogleMapsApiKey = settingsItem.InnerText;
                        break;
                    case "ReplaceLinksTo":
                        ReplaceLinksTo = settingsItem.InnerText;
                        break;
                    case "language":
                        language = string.IsNullOrWhiteSpace(settingsItem.InnerText) ? "english" : settingsItem.InnerText;
                        break;
                    case "taskCount":
                        int.TryParse(settingsItem.InnerText, out TaskCount);
                        break;
                    case "textEditorPath":
                        textEditorPath = settingsItem.InnerText;
                        break;
                    case "textEditorArgs":
                        textEditorArgs = settingsItem.InnerText;
                        break;
                    case "searchAssetsFiles":
                        bool.TryParse(settingsItem.InnerText, out searchAssetsFiles);
                        break;
                    case "searchLibFiles":
                        bool.TryParse(settingsItem.InnerText, out searchLibFiles);
                        break;
                    case "debug":
                        bool.TryParse(settingsItem.InnerText, out writeDebug);
                        break;
                    case "delDebugLog":
                        bool.TryParse(settingsItem.InnerText, out deleteDebug);
                        break;
                }
            }
        }

        //сохранение настроек
        public static void Save()
        {
            string settingsPath = Path.Combine(Program.pathToMyPluginDir, "settings.xml");
            if (!File.Exists(settingsPath)) return;

            XmlDocument doc = new XmlDocument();
            doc.Load(settingsPath);
            XmlNode root = doc.SelectSingleNode("root");
            XmlNode settings = root?.SelectSingleNode("settings");
            if (settings == null) return;

            foreach (XmlNode settingsItem in settings)
            {
                if (settingsItem.NodeType == XmlNodeType.Comment || settingsItem.Attributes == null || settingsItem.Attributes.Count == 0) 
                    continue;

                switch (settingsItem.Attributes[0].Value)
                {
                    case "language":
                        settingsItem.InnerText = language;
                        break;
                    case "debug":
                        settingsItem.InnerText = writeDebug.ToString();
                        break;
                    case "delDebugLog":
                        settingsItem.InnerText = deleteDebug.ToString();
                        break;
                }
            }

            doc.Save(settingsPath);
        }
    }
}