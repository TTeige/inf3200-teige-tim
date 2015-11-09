using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiT.Inf3200.P2PNode
{
    public class LeaderElectionState
    {
        public Guid Guid { get; set; }

        public DateTime CreationTime { get; set; }

        public ConcurrentDictionary<Guid, Guid?> FirstYoAdvertisements { get; }

        public ConcurrentDictionary<Guid, bool?> SecondYoResponses { get; }

        public Guid FirstYoMinimum { get; set; }

        public LeaderElectionState(IEnumerable<Guid> incomingGuids, IEnumerable<Guid> outgoingGuids)
        {
            FirstYoAdvertisements = new ConcurrentDictionary<Guid, Guid?>();
            foreach (var inGuid in incomingGuids)
            {
                FirstYoAdvertisements[inGuid] = null;
            }
            SecondYoResponses = new ConcurrentDictionary<Guid, bool?>();
            foreach (var outGuid in outgoingGuids)
            {
                FirstYoAdvertisements[outGuid] = null;
            }
        }
    }
}
