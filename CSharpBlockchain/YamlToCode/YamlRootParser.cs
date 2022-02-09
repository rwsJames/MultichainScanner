using System.Collections.Generic;

namespace CSharpBlockchain.YamlToCode
{
    using Token = KeyValuePair<string, string>;
    using Tokens = IDictionary<string, string>;

    public class YamlRootParser
    {
        public Tokens tokens;
        public IEnumerable<PreNetwork> nets;

        public YamlRootParser()
        {
            tokens = new Dictionary<string, string>();
            nets = new List<PreNetwork>();
        }

        public void PopulateManager()
        {
            foreach (string key in tokens.Keys)
            {
                SystemManager.Add(key, tokens[key]);
            }
            foreach (PreNetwork preNet in nets)
            {
                SystemManager.Add(new Network(preNet));
            }
        }
    }
}
