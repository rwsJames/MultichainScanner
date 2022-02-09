using CSharpBlockchain.YamlToCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace CSharpBlockchain
{
    using Token = KeyValuePair<string, string>;
    using Tokens = IDictionary<string, string>;
    using Balance = KeyValuePair<KeyValuePair<string, string>, double>;
    using Balances = IDictionary<KeyValuePair<string, string>, double>;

    public class Network
    {
        public string Name;
        public string Explorer;
        public string ApiKey;
        public IEnumerable<Pool> Pools, Dexs;

        public Network() 
        {
            Pools = new List<Pool>();
            Dexs = new List<Pool>();
        }

        public Network(PreNetwork preNet)
        {
            Name = preNet.name;
            Explorer = preNet.explorer;
            ApiKey = preNet.apikey;
            Pools = preNet.GetLiquidityPools();
            Dexs = preNet.GetDexPools();
            AssignAllPoolNetworksToThis();
        }

        public void UpdateAllPools()
        {
            using HttpClient client = new HttpClient
            {
                BaseAddress = new Uri(Explorer)
            };
            foreach (Pool p in Dexs.Concat(Pools))
            {
                try
                {
                    p.UpdateBalances(client);
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private void AssignAllPoolNetworksToThis()
        {
            foreach (Pool pool in Dexs.Concat(Pools))
                pool.Network = this;
        }

        public Pool GetBestDexFor(Token from, Token to)
        {
            Pool best = Dexs.ToArray()[0];
            foreach (Pool p in Dexs.Skip(1))
            {
                if (p.GetRatioBetween(from, to) > best.GetRatioBetween(from, to))
                    best = p;
            }

            return best;
        }

        // Best price froom one token to any one of the others 
        // (where these are like tokens, e.g. USDT, USDC, and DAI OR WBTC, RBTC, etc.)
        public Pool GetBestDexFor(Token from, Tokens multiTo)
        {
            Pool best = Dexs.ToArray()[0];
            foreach (Pool p in Dexs.Skip(1))
            {
                foreach (Token to in multiTo)
                    if (p.GetRatioBetween(from, to) > best.GetRatioBetween(from, to))
                        best = p;
            }

            return best;
        }

        // output the used token from multiTo
        public Pool GetBestDexFor(Token from, Tokens multiTo, out Token usedToken)
        {
            Pool best = Dexs.ToArray()[0];
            Token bestTo = best.Tokens.ToArray()[0];
            foreach (Pool p in Dexs.Skip(1))
            {
                foreach (Token to in multiTo)
                    if (p.Tokens.Contains(to) && p.GetRatioBetween(from, to) > best.GetRatioBetween(from, to))
                    {
                        best = p;
                        bestTo = to;
                    }
            }

            usedToken = bestTo;
            return best;
        }

        public bool IsTokenInNetworkDexs(Token t)
        {
            foreach (Pool p in Dexs)
                if (p.Tokens.Contains(t))
                    return true;

            return false;
        }

        public bool IsTokenInNetworkPools(Token t)
        {
            foreach (Pool p in Pools)
                if (p.Tokens.Contains(t))
                    return true;

            return false;
        }

        public bool IsTokenInNetworkPoolsAndDexs(Token t)
        {
            return IsTokenInNetworkDexs(t) && IsTokenInNetworkPools(t);
        }

        public IEnumerable<Pool> GetPoolsWithToken(Token token)
        {
            IList<Pool> ps = new List<Pool>();
            foreach (Pool p in Pools)
                if (p.Tokens.Contains(token))
                    ps.Add(p);

            return ps;
        }

        public TupleList<Token, double> GetDollarPricesOf(Token token, out IList<Pool> poolsUsed, out IList<Pool> dexsUsed)
        {
            TupleList<Token, double> otherTokensAsDollars = new TupleList<Token, double>();
            TupleList<Token, double> otherTokenRatios = new TupleList<Token, double>();
            Token dollarUsed;

            poolsUsed = new List<Pool>();
            dexsUsed = new List<Pool>();

            if (IsTokenInNetworkPools(token))
            {
                // Get the tokens on the other side of the pairs and their ratio to the param
                foreach (Pool p in GetPoolsWithToken(token))
                    foreach (Token t in p.Tokens)
                        if (!t.Equals(token))
                        {
                            otherTokenRatios.Add(t, p.GetRatioBetween(token, t));
                            poolsUsed.Add(p);
                        }

                // Get dollar ratios for the other-side tokens
                foreach (Tuple<Token, double> otherTokenRatio in otherTokenRatios)
                {
                    // If the original pair was with a dollar then immediately add it to the list
                    if (SystemManager.GetTokensWithMetaDataLabelOf("isdollar").Contains(otherTokenRatio.Item1))
                    {
                        otherTokensAsDollars.Add(otherTokenRatio);
                        dexsUsed.Add(new Pool() { Name = "no dex" });
                        continue;
                    }

                    Pool bestDex = GetBestDexFor(
                        otherTokenRatio.Item1,
                        SystemManager.GetTokensWithMetaDataLabelOf("isdollar"),
                        out dollarUsed);
                    dexsUsed.Add(bestDex);

                    double ratioOfOtherToDollar = bestDex.GetRatioBetween(otherTokenRatio.Item1, dollarUsed);
                    if (ratioOfOtherToDollar == 0)
                        Console.WriteLine($"Could not find a dex with the token {otherTokenRatio.Item1}");
                    double ratioOfParamToDollar = ratioOfOtherToDollar * otherTokenRatio.Item2;

                    if (ratioOfParamToDollar != 8 && ratioOfParamToDollar != 0 && ratioOfParamToDollar != double.PositiveInfinity)
                        SystemManager.Prices.Add(ratioOfParamToDollar);
                    otherTokensAsDollars.Add(otherTokenRatio.Item1, ratioOfParamToDollar);
                }
            }
            return otherTokensAsDollars;
        }

        public double ConvertTo(Token initial, double amount, Token target)
        {
            return ConvertTo(new Balance(initial, amount), target);
        }

        public double ConvertTo(Balance bal, Token target)
        {
            if (IsTokenInNetworkDexs(target) && IsTokenInNetworkPools(bal.Key))
            {
                double best = 0;
                foreach (Pool pool in GetDexsWithToken(target))
                {
                    best = pool.GetRatioBetween() > best ? pool.GetRatioBetween() : best;
                }
                return best;
            }

            return 0;
        }

        public IEnumerable<Balance> GetDollarValuesOfPoolsFromToken(Token initial)
        {
            List<Balance> ds = new List<Balance>();
            foreach (Pool pool in Pools)
            {
                foreach (Balance bal in pool.Balances)
                    if (bal.Key.Equals(initial))
                        ds.Add(new Balance(initial, ConvertTo(bal, initial)));
            }

            return ds;
        }

        public IEnumerable<Pool> GetDexsWithToken(Token token)
        {
            IList<Pool> pools = new List<Pool>();
            if (IsTokenInNetworkPools(token))
                foreach (Pool p in Dexs)
                    if (p.Tokens.Contains(token))
                        pools.Add(p);

            return pools;
        }

    }
}
