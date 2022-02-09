using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpBlockchain
{
    using Token = KeyValuePair<string, string>;
    using Tokens = IDictionary<string, string>;
    using Balance = Tuple<KeyValuePair<string, string>, double>;
    using Balances = TupleList<KeyValuePair<string, string>, double>;

    public static class SystemManager
    {
        public static List<double> Prices = new List<double>();
        public static IList<Network> Networks = new List<Network>();
        public static Tokens Tokens = new Dictionary<string, string>();
        public static IDictionary<Token, Tokens> TokenMetaData = new Dictionary<Token, Tokens>();

        public static void UpdateNetworks()
        {
            if (Prices.Count > 100)
                Prices.RemoveRange(0, 100/2);


            foreach (Network net in Networks)
            {
                net.UpdateAllPools();
            }
        }

        public static void Add(Network net)
        {
            Networks.Add(net);
        }

        public static void Add(string symbol, string addressAndMetaData)
        {
            if (addressAndMetaData.Contains('-'))
            {
                string[] splitString = addressAndMetaData.Split('-');
                string address = splitString[0];
                Token toAdd = new Token(symbol, address);
                Tokens.Add(toAdd);

                Tokens metadata = new Dictionary<string, string>();
                foreach (string meta in splitString.Skip(1))
                {
                    if (meta.Contains('='))
                    {
                        string[] innerSplitString = meta.Split('=');
                        metadata.Add(innerSplitString[0], innerSplitString[1]);
                    }
                    else
                        throw new FormatException($"Invalid format for token metadata in yaml\nProblem string: {meta} || Token: {symbol}" +
                            $"\nFormat should be [0x Address]-[name of metadata]=[data for metadata]-[name of metadata]=[data for metadata]");
                }
                TokenMetaData.Add(toAdd, metadata);
            }
            else // there is no metadata accompanying the address
            {
                Tokens.Add(symbol, addressAndMetaData);
            }
        }

        // Fetch a token's specific meta data value frm the manager
        public static string GetMetaDataValue(Token t, string key)
        {
            return TokenMetaData[t][key];
        }

        // TODO: move some verification here
        // Fetch all the meta data for a specific token
        public static Tokens GetMetaDataDict(Token t)
        {
            return TokenMetaData[t];
        }

        public static void PrintAll()
        {
            PrintNetsAndPools();
            PrintAllTokens();
        }

        public static void PrintNetsAndPools()
        {
            StringBuilder sb = new StringBuilder(1024);
            foreach (Network net in Networks)
            {
                sb.AppendLine($"| {net.Name.ToUpper()}: ");
                sb.AppendLine($"\t| Explorer: {net.Explorer}");

                sb.AppendLine($"\t| Dexs: ");
                foreach (Pool pool in net.Dexs)
                {
                    sb.AppendLine($"\t\t| {pool.Address} : {pool.GetRatioBetween():N5}");
                    foreach (Token token in pool.Tokens)
                    {
                        sb.AppendLine($"\t\t\t| {token.Key} : {token.Value}");
                        sb.AppendLine($"\t\t\t\t| {pool.Balances[token]:N5} {token.Key}");
                    }
                }

                sb.AppendLine($"\t| Pools: ");
                foreach (Pool pool in net.Pools)
                {
                    sb.AppendLine($"\t\t| {pool.Address} : {pool.GetRatioBetween():N5}");
                    foreach (Token token in pool.Tokens)
                    {
                        if (pool.Balances.ContainsKey(token))
                        {
                            sb.AppendLine($"\t\t\t| {token.Key} : {token.Value}");
                            sb.AppendLine($"\t\t\t\t| {pool.Balances[token]:N5} {token.Key}");
                        }
                    }
                }
                sb.AppendLine();
            }
            Console.WriteLine(sb.ToString());
        }

        public static Token Find(string symbol)
        {
            foreach (Token token in Tokens)
                if (token.Key.Equals(symbol))
                    return token;

            return new Token("null", "null");
        }

        public static void PrintAllTokens()
        {
            StringBuilder sb = new StringBuilder(1024);
            foreach (Token t in Tokens)
            {
                sb.AppendLine($"| {t.Key}");
                sb.AppendLine($"\t| {t.Value}");
                if (TokenMetaData.ContainsKey(t))
                    foreach (Token tt in TokenMetaData[t])
                        sb.AppendLine($"\t\t| {tt.Key}={tt.Value}");
            }
            Console.WriteLine(sb.ToString());
        }

        public static Tokens GetTokensWithMetaDataLabelOf(string label)
        {
            Tokens tokens = new Dictionary<string, string>();
            foreach (Token t in Tokens)
                if (TokenMetaData.ContainsKey(t))
                    if (TokenMetaData[t].ContainsKey(label))
                        tokens.Add(t);

            return tokens;
        }

        public static void PrintBetter(bool doAverage = true, params string[] symbols)
        {
            StringBuilder sb = new StringBuilder(2048);

            if (doAverage)
                sb.AppendLine($"Average = {GetMeanPrice()}\n");

            bool oneGreaterThan = false, oneLesserthan = false;
            foreach (Network net in Networks)
            {
                sb.AppendLine($"{net.Name}:");

                foreach (string symbol in symbols)
                {
                    Token searchToken = Find(symbol);
                    Balances bals = net.GetDollarPricesOf(searchToken, out IList<Pool> poolsUsed, out IList<Pool> dexsUsed);
                    if (bals.Count != 0) // only do work if this is the right network for the token
                    {
                        
                        foreach (Balance bal in bals)
                        {
                            // Beep once if either of these are true to notify the user
                            if (doAverage && bal.Item2 > GetMeanPrice() * 1.025)
                            {
                                oneGreaterThan = true;
                                Console.Beep();

                                PrintWithColourInline(
                                    $"!!!!!SELL ON {net.Name}!!!!!",
                                    ConsoleColor.White,
                                    ConsoleColor.DarkGreen,
                                    "\n\t", "\t\n");
                            }
                            else if (doAverage && bal.Item2 < GetMeanPrice() * 0.975)
                            {
                                oneLesserthan = true;
                                Console.Beep();

                                PrintWithColourInline(
                                    $"!!!!!BUY ON {net.Name}!!!!!",
                                    ConsoleColor.White,
                                    ConsoleColor.DarkRed,
                                    "\n\t", "\t\n");
                            }

                            // Beep on every subsequent loop (i.e. at least thrice per loop) to alert the user
                            if (oneGreaterThan && oneLesserthan)
                                Console.Beep();
                        }

                        for (int i = 0; i < dexsUsed.Count; i++)
                        {
                            sb.AppendLine($"\t{symbol} = {bals.ToArray()[i].Item2:N3} <<< from ({poolsUsed.ToArray()[i].Name}) to " +
                            $"dex ({dexsUsed[i].Name})");
                        }
                    }
                }
            }

            sb.AppendLine("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-");
            Console.WriteLine(sb.ToString());
        }

        // Preceding and appended strings are -not- coloured
        public static void PrintWithColourInline(
            string str, 
            ConsoleColor fgCol = ConsoleColor.Gray, 
            ConsoleColor bgCol = ConsoleColor.Black, 
            string precedingString = "", 
            string appendedString = "")
        {
            Console.Write(precedingString);
            Console.ForegroundColor = fgCol;
            Console.BackgroundColor = bgCol;
            Console.Write(str);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write(appendedString);
        }

        public static double GetMeanPrice()
        {
            return Prices.Sum() / Prices.Count();
        }

    }
}
