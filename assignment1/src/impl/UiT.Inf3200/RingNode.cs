using System;
using System.Xml.Serialization;

namespace UiT.Inf3200
{
    [XmlType, Serializable]
    public class RingNode
    {
        [XmlAttribute("ringId")]
        public int RingId { get; set; }

        [XmlAttribute("nodeGuid")]
        public Guid NodeGuid { get; set; }

        [XmlAttribute("nodeUri")]
        public string NodeUri { get; set; }
    }
}