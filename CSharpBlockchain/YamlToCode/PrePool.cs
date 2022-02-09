using System.Collections.Generic;

namespace CSharpBlockchain.YamlToCode
{
    using Token = KeyValuePair<string, string>;
    using Tokens = IDictionary<string, string>;

    public class PrePool
    {
        public string pair;
        public string address;
        public string name = "";

        public Pool ToPool()
        {
            return new Pool()
            {
                Tokens = GetTokens(),
                Address = address,
                Name = name,
            };
        }

        public Tokens GetTokens()
        {
            Tokens tokens = new Dictionary<string, string>();
            foreach (string s in pair.Split('-'))
                if (SystemManager.Tokens.ContainsKey(s))
                    tokens.Add(s, SystemManager.Tokens[s]);

            return tokens;
        }
    }
}