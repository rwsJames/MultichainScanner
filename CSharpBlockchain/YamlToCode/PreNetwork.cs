using System.Collections.Generic;

namespace CSharpBlockchain.YamlToCode
{
    public class PreNetwork
    {
        public string name;
        public string explorer;
        public IEnumerable<PrePool> dexs = new List<PrePool>();
        public IEnumerable<PrePool> pools;
        public string apikey = "";

        public IEnumerable<Pool> GetLiquidityPools()
        {
            return PrePoolsToPools(pools);
        }

        public IEnumerable<Pool> GetDexPools()
        {
            return PrePoolsToPools(dexs);
        }

        private IEnumerable<Pool> PrePoolsToPools(IEnumerable<PrePool> pres)
        {
            List<Pool> pools = new List<Pool>();
            foreach (PrePool pre in pres)
            {
                pools.Add(pre.ToPool());
            }
            return pools;
        }
    }
}
