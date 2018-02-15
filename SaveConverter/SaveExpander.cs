using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SaveConverter
{
    public class SaveExpander
    {

        public static void ExpandSave(string path, string outputPath, ConvertSettings settings)
        {

            if (settings.singleFile)
                ExpandSaveSingleFile(path, outputPath ?? path + ".json", settings);
            else
                ExpandSaveMultipleFiles(path, outputPath ?? path + "_json", settings);
        }

        private static void ExpandSaveMultipleFiles(string path, string outputPath, ConvertSettings settings)
        {
            string scenesPath = Path.Combine(outputPath, "scenes");

            string decompressedJson = Encoding.UTF8.GetString(CLZF.Decompress(File.ReadAllBytes(path)));
            SaveDataProxy save = JsonConvert.DeserializeObject<SaveDataProxy>(decompressedJson);
            decompressedJson = null;

            if(Directory.Exists(outputPath))
                Directory.Delete(outputPath, true);
            Directory.CreateDirectory(outputPath);
            Directory.CreateDirectory(scenesPath);

            while (save.m_Dict.Keys.Count != 0)
            {
                string key = save.m_Dict.Keys.First();
                var val = ExpandFile(save.m_Dict[key]);
                if (settings.omitNull || settings.omitFalse || settings.omitZero)
                    Util.RemoveDefault(val, settings.omitNull, settings.omitFalse, settings.omitZero);
                string filePath;
                if (key == "global" || key == "boot" || key == "screenshot")
                    filePath = Path.Combine(outputPath, key + ".json");
                else
                    filePath = Path.Combine(scenesPath, key + ".json");

                File.WriteAllText(filePath, JsonConvert.SerializeObject(val, settings.minify ? Formatting.None : Formatting.Indented));
                save.m_Dict.Remove(key);
            }

            File.WriteAllText(Path.Combine(outputPath, "slotData.json"), JsonConvert.SerializeObject(save, settings.minify ? Formatting.None : Formatting.Indented));

        }

        private static void ExpandSaveSingleFile(string path, string outputPath, ConvertSettings settings)
        {
            string decompressedJson = Encoding.UTF8.GetString(CLZF.Decompress(File.ReadAllBytes(path)));
            SaveDataProxy save = JsonConvert.DeserializeObject<SaveDataProxy>(decompressedJson);
            decompressedJson = null;

            // TODO JObject.FromObject
            var result = new ExpandedSaveDataProxy
            {
                m_BaseName = save.m_BaseName,
                m_DisplayName = save.m_DisplayName,
                m_Episode = save.m_Episode,
                m_GameId = save.m_GameId,
                m_GameMode = save.m_GameMode,
                m_IsPS4Compliant = save.m_IsPS4Compliant,
                m_Name = save.m_Name,
                m_Timestamp = save.m_Timestamp,
                m_Dict = new Dictionary<string, dynamic>()
            };

            while (save.m_Dict.Keys.Count != 0)
            {
                string key = save.m_Dict.Keys.First();
                var val = ExpandFile(save.m_Dict[key]);
                if (settings.omitNull || settings.omitFalse || settings.omitZero)
                    Util.RemoveDefault(val, settings.omitNull, settings.omitFalse, settings.omitZero);
                result.m_Dict[key] = val;
                save.m_Dict.Remove(key);
            }

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                FloatFormatHandling = FloatFormatHandling.Symbol,
                Formatting = Formatting.Indented
            };

            File.WriteAllText(outputPath, JsonConvert.SerializeObject(result));
        }

        private static JToken ExpandFile(byte[] data)
        {
            string decompressedJson = Encoding.UTF8.GetString(CLZF.Decompress(data));
            if (decompressedJson == null)
                return null;

            decompressedJson = Util.UnShittifyJson(decompressedJson);
            JToken fileToken = JToken.Parse(decompressedJson);
            decompressedJson = null;

            Expand(fileToken);

            return fileToken;
        }

        private static void Expand(JToken token)
        {
            var type = token.GetType();

            if (type == typeof(JObject))
            {
                JObject JObj = (JObject)token;
                foreach (var jo in JObj)
                {
                    Expand(jo.Value);
                    if (Util.dontConvert.Contains(jo.Key))
                        continue;
                    else if (jo.Key == "m_SerializedMissions")
                    {
                        var arr = (JArray)jo.Value;
                        for (var i = 0; i < arr.Count(); i += 2)
                        {
                            arr[i + 1].Replace(ExpandJValue((JValue)arr[i + 1]));
                        }
                    }
                    else if (jo.Key == "m_SerializedConcurrentGraphs")
                    {
                        var arr = (JArray)jo.Value;
                        for (var i = 0; i < arr.Count(); i += 4)
                        {
                            arr[i + 2].Replace(ExpandJValue((JValue)arr[i + 2]));
                            arr[i + 3].Replace(ExpandJValue((JValue)arr[i + 3]));
                        }
                    }
                    else if (jo.Key == "m_SerializedTimers")
                    {
                        var arr = (JArray)jo.Value;
                        for (var i = 0; i < arr.Count(); i += 3)
                        {
                            arr[i + 2].Replace(ExpandJValue((JValue)arr[i + 2]));
                        }
                    }
                    else if (Util.convertArrayValues.Contains(jo.Key))
                    {
                        var arr = (JArray)jo.Value;
                        for (var i = 0; i < arr.Count(); i++)
                        {
                            var jt = arr[i];
                            if (jt.GetType() == typeof(JValue))
                            {
                                jt.Replace(ExpandJValue((JValue)jt));
                            }
                        }
                    }
                    else if (Util.convertChildArraysAndArrayValues.Contains(jo.Key))
                    {
                        var jObject = (JObject)jo.Value;
                        foreach (var jo2 in jObject)
                        {
                            var newToken = JArray.Parse((string)((JValue)jo2.Value).Value);
                            for (var i = 0; i < newToken.Count; i++)
                            {
                                newToken[i].Replace(ExpandJValue((JValue)newToken[i]));
                            }
                            jo2.Value.Replace(newToken);
                        }
                    }
                    else if (Util.convertChildValues.Contains(jo.Key))
                    {
                        var obj = (JObject)jo.Value;
                        foreach (var kvp2 in obj)
                        {
                            kvp2.Value.Replace(ExpandJValue((JValue)kvp2.Value));
                        }
                    }
                    else if ((jo.Key.Contains("Serialized") || jo.Key.Contains("Searialized") || Util.convert.Contains(jo.Key)) && jo.Value.GetType() == typeof(JValue))
                    {

                        jo.Value.Replace(ExpandJValue((JValue)jo.Value));
                    }
                }
            }
            else if (type == typeof(JArray))
            {
                JArray JArr = (JArray)token;
                foreach (var jo in JArr)
                {
                    Expand(jo);
                }
            }
        }

        private static JToken ExpandJValue(JValue val)
        {
            var json = (string)val.Value;

            if (string.IsNullOrEmpty(json))
                return json;

            var newToken = JToken.Parse(json);
            json = null;
            Expand(newToken);
            return newToken;
        }
    }
}
