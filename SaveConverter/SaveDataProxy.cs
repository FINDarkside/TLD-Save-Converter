using System.Collections.Generic;

namespace SaveConverter
{
    public class SaveDataProxy
    {
        public dynamic m_Name;
        public dynamic m_BaseName;
        public dynamic m_DisplayName;
        public dynamic m_Timestamp;
        public dynamic m_GameMode;
        public dynamic m_GameId;
        public dynamic m_Episode;
        public Dictionary<string, byte[]> m_Dict;
        public dynamic m_IsPS4Compliant;
    }

    public class ExpandedSaveDataProxy
    {
        public dynamic m_Name;
        public dynamic m_BaseName;
        public dynamic m_DisplayName;
        public dynamic m_Timestamp;
        public dynamic m_GameMode;
        public dynamic m_GameId;
        public dynamic m_Episode;
        public Dictionary<string, dynamic> m_Dict;
        public dynamic m_IsPS4Compliant;
    }
}
