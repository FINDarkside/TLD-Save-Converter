using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SaveConverter
{
    public static class Util
    {

        public static HashSet<string> convertChildArraysAndArrayValues = new HashSet<string>() { "m_DetailSurveyPositions", "m_MapSaveDataDict" };
        public static HashSet<string> convertArrayValues = new HashSet<string>() { "strings", "m_Trails", "m_FootstepGroups", "m_SerializedChildGraphs" };
        public static HashSet<string> convertChildValues = new HashSet<string>() { "m_NeedTrackersSerialized", "m_UnlockableTrackersSerialized" };
        public static HashSet<string> dontConvert = new HashSet<string>() { "m_ObjectGuidSerialized", "m_SerializedFires", "m_SerializedItems", "m_SerializedContainers", "m_SerializedBodyHarvests", "m_SerializedContainers", "m_SerializedWaterSources", "m_GearToInstantiateSerialized", "m_SerializedChimneyData", "m_SerializedSpawnRegions", "m_SerializedDecalProjectors", "m_SerializedSnowShelters", "m_SerializedBaseAI", "m_SerializedFields"};
        public static HashSet<string> convert = new HashSet<string>() { "m_GearSpawnInOldSavesGUIDs", "m_Blackboard" };

        // <summary>Fix invalid json inside all m_StatsDictionary instances (numbers as keys)</summary>
        public static string UnShittifyJson(string json)
        {
            int currentIndex = 0;
            while (true)
            {
                int statsDictStartIndex = json.IndexOf("m_StatsDictionary", currentIndex);
                if (statsDictStartIndex == -1)
                    break;
                statsDictStartIndex = json.IndexOf('{', statsDictStartIndex);
                currentIndex = statsDictStartIndex;

                int statsDictEndIndex = json.IndexOf('}', statsDictStartIndex);

                var newStats = json.Substring(statsDictStartIndex, statsDictEndIndex - statsDictStartIndex);
                if (newStats.Length <= 2)
                    continue;
                int depth = 0;
                int quoteIndex = newStats.IndexOf("\"");
                for (int i = quoteIndex - 1; newStats[i] == '\\'; i--, depth++) { }
                string escapes = new String('\\', depth);

                for (var i = 0; i < newStats.Length; i++)
                {
                    var c = newStats[i];

                    if ((c == '-' || char.IsDigit(c)))
                    {
                        if (newStats[i - 1] != '"' && newStats[i - 1] != '.')
                        {
                            newStats = newStats.Insert(i, escapes + "\"");
                            i += 2 + depth;

                            while (char.IsDigit(newStats[i]) || newStats[i] == '-') { i++; }
                            newStats = newStats.Insert(i, escapes + "\"");
                            i += 1 + depth;
                        }
                        else
                        {
                            while (char.IsDigit(newStats[i]) || newStats[i] == '.' || newStats[i] == 'E' || newStats[i] == '-') { i++; }
                        }
                    }
                }

                json = json.Substring(0, statsDictStartIndex) + newStats + json.Substring(statsDictEndIndex);
            }

            return json;
        }

        // <summary>Make m_StatsDictionarys into invalid json (numbers as keys)</summary>
        public static string ShittifyJson(string json)
        {
            // And of course the game can't read that valid json so we have to fuck it up again
            int currentIndex = 0;
            while (true)
            {
                int statsDictStartIndex = json.IndexOf("m_StatsDictionary", currentIndex);
                if (statsDictStartIndex == -1)
                    break;
                statsDictStartIndex = json.IndexOf('{', statsDictStartIndex);
                currentIndex = statsDictStartIndex;

                int statsDictEndIndex = json.IndexOf('}', statsDictStartIndex);

                var newStats = json.Substring(statsDictStartIndex, statsDictEndIndex - statsDictStartIndex);
                if (newStats.Length <= 2)
                    continue;

                int currentIndex2 = 0;
                while (true)
                {
                    int colonIndex = newStats.IndexOf(':', currentIndex2);
                    if (colonIndex == -1)
                        break;
                    currentIndex2 = colonIndex + 1;

                    int i = colonIndex;
                    while (newStats[i] != '{' && newStats[i] != ',')
                    {
                        if (newStats[i] == '\\' || newStats[i] == '\"')
                            newStats = newStats.Remove(i, 1);
                        i--;
                    }
                }

                json = json.Substring(0, statsDictStartIndex) + newStats + json.Substring(statsDictEndIndex);
            }

            return json;
        }

        public static void RemoveDefault(JToken token, bool removeNull, bool removeFalse, bool removeZero)
        {
            
            if (token is JObject jObject)
            {
                jObject.Properties().ToList().ForEach(item => RemoveDefault(item, removeNull, removeFalse, removeZero));
            }
            else if (token is JArray jArr)
            {
                jArr.ToList().ForEach(item => RemoveDefault(item, removeNull, removeFalse, removeZero));
            }
            else if (token is JValue value)
            {
                if (token.Parent.Type != JTokenType.Property)
                    return;

                if ((value.Type == JTokenType.Null || value.Value == null) && removeNull)
                    token.Parent.Remove();
                else if (removeZero && (value.Type == JTokenType.Float || token.Type == JTokenType.Integer) && value.Value<double>() == 0)
                    token.Parent.Remove();
                else if (value.Type == JTokenType.Boolean && removeFalse && !value.Value<bool>())
                    token.Parent.Remove();
            }
            else if (token is JProperty)
            {
                RemoveDefault(((JProperty)token).Value, removeNull, removeFalse, removeZero);
            }
        }

    }

}
