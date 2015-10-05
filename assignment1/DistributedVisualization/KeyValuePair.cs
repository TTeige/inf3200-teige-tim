using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace UiT.Inf3200.StorageNodeServer
{
    [XmlType, Serializable]
    class KeyValuePair
    {
        [XmlAttribute("key")]
        public string Key { get; set; }

        [XmlAttribute("value")]
        public byte[] Value { get; set; }
    }
}
