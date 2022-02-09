using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpBlockchain
{
    class PreNetwork
    {
        public string name;
        public string explorer;
        public IList<PrePool> pools;
        public IList<string> poolAddresses;

        public Network ToNetwork()
        {
            return new Network()
            {
                Name = name,
                Explorer = explorer,
                TokenAddresses = tokenAddresses,
                Pools = Network.StringsToPools(poolAddresses),
            };
        }
    }
}
