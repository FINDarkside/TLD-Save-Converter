using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaveConverter
{
    public class SaveCompressor
    {

        public static void CompressSave(string path, string outputPath)
        {
            if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
                CompressMultipleFiles(path, outputPath);
            else
                CompressSingleFile(path, outputPath);

        }

        private static void CompressSingleFile(string path, string outputPath)
        {
            JObject obj = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(path));
            Compress(obj);
            var result = new SaveDataProxy
            {
                m_BaseName = obj["m_BaseName"],
                m_DisplayName = obj["m_DisplayName"],
                m_Episode = obj["m_Episode"],
                m_GameId = obj["m_GameId"],
                m_GameMode = obj["m_GameMode"],
                m_IsPS4Compliant = obj["m_IsPS4Compliant"],
                m_Name = obj["m_Name"],
                m_Timestamp = obj["m_Timestamp"],
                m_Dict = new Dictionary<string, byte[]>()
            };

            var dict = (JObject)obj["m_Dict"];
            foreach (var jo in dict)
            {
                var fileJson = JsonConvert.SerializeObject(jo.Value, Formatting.None);
                fileJson = Util.ShittifyJson(fileJson);
                result.m_Dict[jo.Key.ToString()] = CLZF.Compress(Encoding.UTF8.GetBytes(fileJson));
                jo.Value.Replace(null);
            }

            var resultJson = JsonConvert.SerializeObject(result, Formatting.None);
            File.WriteAllBytes(outputPath, CLZF.Compress(Encoding.UTF8.GetBytes(resultJson)));
        }

        private static void CompressMultipleFiles(string path, string outputPath)
        {

            string scenesPath = Path.Combine(path, "scenes");
            string slotDataPath = Path.Combine(path, "slotData.json");

            SaveDataProxy slot = JsonConvert.DeserializeObject<SaveDataProxy>(File.ReadAllText(slotDataPath));
            slot.m_Dict["global"] = ReadJToken(Path.Combine(path, "global.json"));
            slot.m_Dict["boot"] = ReadJToken(Path.Combine(path, "boot.json"));
            slot.m_Dict["screenshot"] = ReadJToken(Path.Combine(path, "screenshot.json"));

            foreach (var file in Directory.GetFiles(scenesPath))
            {
                slot.m_Dict[Path.GetFileNameWithoutExtension(file)] = ReadJToken(file);
            }

            var resultJson = JsonConvert.SerializeObject(slot, Formatting.None);
            File.WriteAllBytes(outputPath, CLZF.Compress(Encoding.UTF8.GetBytes(resultJson)));
        }

        private static byte[] ReadJToken(string path)
        {
            JToken token = JToken.Parse(File.ReadAllText(path));
            Compress(token);
            var fileJson = JsonConvert.SerializeObject(token, Formatting.None);
            var json = Util.ShittifyJson(fileJson);
            return CLZF.Compress(Encoding.UTF8.GetBytes(json));
        }

        private static void Compress(JToken token)
        {
            var type = token.GetType();

            if (type == typeof(JObject))
            {
                JObject JObj = (JObject)token;
                foreach (var jo in JObj)
                {
                    if(jo.Key == "m_PlayerManagerSerialized")
                    {
                        jo.Value["m_CheatsUsed"] = true;
                    }

                    Compress(jo.Value);
                    if (Util.dontConvert.Contains(jo.Key))
                        continue;
                    else if (Util.convertChildArraysAndArrayValues.Contains(jo.Key))
                    {
                        var jObject = (JObject)jo.Value;
                        foreach (var jo2 in jObject)
                        {
                            var arr = (JArray)jo2.Value;
                            for (var i = 0; i < arr.Count; i++)
                            {
                                if (arr[i].Type != JTokenType.Null)
                                    arr[i].Replace(JsonConvert.SerializeObject(arr[i], Formatting.None));
                            }
                            if (arr.Type != JTokenType.Null)
                                jo2.Value.Replace(JsonConvert.SerializeObject(arr, Formatting.None));
                        }
                    }
                    else if (Util.convertArrayValues.Contains(jo.Key))
                    {
                        var arr = (JArray)jo.Value;
                        for (var i = 0; i < arr.Count(); i++)
                        {
                            var jt = arr[i];
                            if (jt.Type != JTokenType.Null)
                                jt.Replace(JsonConvert.SerializeObject(jt, Formatting.None));
                        }
                    }
                    else if (Util.convertChildValues.Contains(jo.Key))
                    {
                        var jObject = (JObject)jo.Value;
                        foreach (var jo2 in jObject)
                        {
                            if (jo2.Value.Type != JTokenType.Null)
                                jo2.Value.Replace(JsonConvert.SerializeObject(jo2.Value, Formatting.None));
                        }
                    }
                    else if (jo.Key == "m_SerializedMissions")
                    {
                        var arr = (JArray)jo.Value;
                        for (var i = 0; i < arr.Count(); i += 2)
                        {
                            if (arr[i + 1].Type != JTokenType.Null)
                                arr[i + 1].Replace(JsonConvert.SerializeObject(arr[i + 1], Formatting.None));
                        }
                    }
                    else if (jo.Key == "m_SerializedConcurrentGraphs")
                    {
                        var arr = (JArray)jo.Value;
                        for (var i = 0; i < arr.Count(); i += 4)
                        {
                            if (arr[i + 2].Type != JTokenType.Null)
                                arr[i + 2].Replace(JsonConvert.SerializeObject(arr[i + 2], Formatting.None));
                            if (arr[i + 3].Type != JTokenType.Null)
                                arr[i + 3].Replace(JsonConvert.SerializeObject(arr[i + 3], Formatting.None));
                        }
                    }
                    else if (jo.Key == "m_SerializedTimers")
                    {
                        var arr = (JArray)jo.Value;
                        for (var i = 0; i < arr.Count(); i += 3)
                        {
                            if (arr[i + 2].Type != JTokenType.Null)
                                arr[i + 2].Replace(JsonConvert.SerializeObject(arr[i + 2], Formatting.None));
                        }
                    }
                    else if (jo.Key.Contains("Serialized") || jo.Key.Contains("Searialized") || Util.convert.Contains(jo.Key))
                    {
                        if (jo.Value.Type != JTokenType.Null)
                            jo.Value.Replace(JsonConvert.SerializeObject(jo.Value, Formatting.None));
                    }
                }
            }
            else if (type == typeof(JArray))
            {
                JArray JArr = (JArray)token;
                foreach (var jo in JArr)
                {
                    Compress(jo);
                }
            }
        }

    }
}
