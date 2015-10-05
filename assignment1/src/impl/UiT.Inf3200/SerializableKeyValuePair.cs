using System;
using System.Xml.Serialization;

namespace UiT.Inf3200
{
    [XmlType, Serializable]
    public class SerializableKeyValuePair
    {
        [XmlAttribute("key")]
        public string Key { get; set; }

        [XmlAttribute("value")]
        public byte[] Value { get; set; }
    }
}
