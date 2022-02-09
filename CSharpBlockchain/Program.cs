using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using CSharpBlockchain.YamlToCode;

namespace CSharpBlockchain
{
    using Token = KeyValuePair<string, string>;
    using Tokens = IDictionary<string, string>;
    using Balance = Tuple<KeyValuePair<string, string>, double>;
    using Balances = TupleList<KeyValuePair<string, string>, double>;

    class Program
    {
        private static IDeserializer _deserializer;
        
        static async Task Main(string[] args)
        {
            Init();

            SystemManager.UpdateNetworks();
            SystemManager.PrintNetsAndPools();

            // Etherscan has a max of 5 requests per second and 100,000 requests daily (on free tier)
            // With a daily limit of 100,000, one request can be sent a minimum of once every
            // (24*60*60) / 100000 = 0.864 seconds per request
            // If only active for [10] hours in the day (e.g.) then your new limit would be
            // (([10]/24)*(24*60*60)) / 100000 = 0.36 seconds per request
            while (true)
            {
                // Processing
                SystemManager.UpdateNetworks();

                // Cleanup
                Console.Clear();

                // Display

                //SystemManager.PrintBetter(true, "ELK");

                //SystemManager.PrintBetter(false, "ETHbsc", "ETHftm", "ETHmatic");
                //SystemManager.PrintBetter(false, "BTCbsc", "BTCftm", "BTCmatic");

                SystemManager.PrintBetter(true, "O3");
            }
        }

        protected static void Init()
        {
            // Setup the console window
            Console.SetWindowSize(90, 50);
            Console.SetBufferSize(90, 50);

            // Parse the PoolAddresses.yaml config file
            _deserializer = new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .Build();

#if DEBUG
            YamlRootParser yamlRootParser =
                _deserializer.Deserialize<YamlRootParser>(File.ReadAllText("../../PoolAddressesO3.yaml"));
#else
            YamlRootParser yamlRootParser = 
                _deserializer.Deserialize<YamlRootParser>(File.ReadAllText("./PoolAddresses.yaml"));
#endif
            yamlRootParser.PopulateManager();
        }
    }
}
