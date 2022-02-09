using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace CSharpBlockchain
{
    using Token = KeyValuePair<string, string>;
    using Tokens = IDictionary<string, string>;
    // differing definition to the preferred non-unique-key list but still functional due to no cross references and being locally unique anyway
    using Balance = KeyValuePair<KeyValuePair<string, string>, double>;
    using Balances = IDictionary<KeyValuePair<string, string>, double>;

    public class Pool
    {
        private static readonly IDeserializer _deserializer = new DeserializerBuilder().Build();

        public Tokens Tokens;
        public Balances Balances = new Dictionary<Token, double>();
        public string Address;
        public Network Network;
        public string Name = "";
        public string Pair 
        {
            get 
            {
                string pair = "";
                foreach (Token t in Tokens)
                    pair += t.Key + "-";

                // remove trailing hyphen
                pair = pair.Substring(0, pair.Length-1);

                return pair;
            }
        }

        public Pool() 
        {
            Tokens = new Dictionary<string, string>();
            Network = new Network();
        }

        public void AddBalance(Token token, double bal)
        {
            if (SystemManager.Tokens.ContainsKey(token.Key))
            {
                // Means there was an error getting the balance as it will always be non-zero on success
                // This check ensures that if one value has been retrieved then an invariant can be calculated anyway
                // TODO: make a setting to allow or not
                if (bal == 0 && Balances.ContainsKey(token))
                {
                    double prevBal = Balances[token];
                    Balances.Remove(token);
                    Balances.Add(token, prevBal);
                }
                else
                {
                    Balances.Remove(token);
                    Balances.Add(token, bal);
                }
            }
            else
                throw new KeyNotFoundException("SystemManager does not contain the key " + token.Key);
        }

        public Balances UpdateBalances(HttpClient client)
        {
            foreach (Token token in Tokens)
            {
                // Get token balance
                string uriParameters = $"?module=account&action=tokenbalance" +
                                       $"&contractaddress={token.Value}" +
                                       $"&address={Address}";
                if (!Network.ApiKey.Equals(""))
                    uriParameters +=   $"&apikey={Network.ApiKey}";

                try 
                {
                    using HttpResponseMessage response = client.GetAsync(uriParameters).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        int decimals = 18; // assumed to have 18 decimals
                        if (SystemManager.TokenMetaData.ContainsKey(token) && SystemManager.TokenMetaData[token].ContainsKey("decimals"))
                            try { decimals = int.Parse(SystemManager.GetMetaDataValue(token, "decimals")); }
                            catch (Exception e) { throw new Exception($"Non-integer value given for number of decimals.\n{e.Message}"); }

                        double balance = 0;
                        try
                        {
                            balance = double.Parse((string)_deserializer.Deserialize<IDictionary>(
                                          response.Content.ReadAsStringAsync().Result)["result"])
                                      / Math.Pow(10, decimals);
                        }
                        catch (FormatException e)
                        {
                            Console.WriteLine($"Bad format when reading fetched balance of {token.Key}" +
                                              $"\n{e.Message}");
                        }
                        
                        AddBalance(token, balance);
                    }
                    else
                        throw new HttpRequestException("Unsuccessful response from " + client.BaseAddress.AbsoluteUri + " with parameters " + uriParameters);
                }
                catch (TaskCanceledException e) 
                {
                    Console.WriteLine("Async task was cancelled, likely due to a timeout.\n" + e.Message);
                    continue;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Some async error.\n" + e.Message);
                    continue;
                }
            }

            return Balances;
        }

        public Balance? GetBalanceOfToken(Token token)
        {
            return Balances.SingleOrDefault(b => b.Key.Equals(token));
        }

        public double GetRatioBetween(Token baseT, Token quoteT)
        {
            if (Has(baseT) && Has(quoteT))
                return Balances[quoteT] / Balances[baseT];
            else 
                return 0;
        }

        public double GetRatioBetween(bool flip = false)
        {
            if (Tokens.Count == 2 
                && Balances.ContainsKey(Tokens.ToArray()[flip.GetHashCode()]) 
                && Balances.ContainsKey(Tokens.ToArray()[(!flip).GetHashCode()]))
                return Balances[Tokens.ToArray()[flip.GetHashCode()]] 
                       / Balances[Tokens.ToArray()[(!flip).GetHashCode()]];
            else
                return 0;
        }

        public bool Has(Token t)
        {
            return Tokens.Contains(t) && Balances.ContainsKey(t);
        }
    }
}
