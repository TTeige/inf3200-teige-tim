using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiT.Inf3200
{
    public static class StorageNodeFinder
    {
        public static Tuple<Guid, Uri> FindStorageNode(int[] keys, int hashCode, IDictionary<int, Guid> nodeRing, IDictionary<Guid, Uri> storageNodes, out bool success)
        {
            int ringId = keys[0];
            if (keys[keys.Length - 1] > hashCode)
                ringId = keys.Reverse().SkipWhile(k => k > hashCode).First();
            Guid nodeGuid;
            Uri nodeUri = null;
            success = nodeRing.TryGetValue(ringId, out nodeGuid) && storageNodes.TryGetValue(nodeGuid, out nodeUri);
            return Tuple.Create(nodeGuid, nodeUri);
        }
    }
}
