using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpBlockchain
{
    class YamlRootParser
    {
        public IList<PreNetwork> nets;

        public YamlRootParser()
        {
            nets = new List<PreNetwork>();
        }

        public void PopulateNetworkManager(NetworkManager nm)
        {
            foreach (PreNetwork preNet in nets)
            {
                nm.Add(preNet.ToNetwork());
            }
        }
    }
}
