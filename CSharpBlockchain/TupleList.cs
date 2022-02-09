using System;
using System.Collections.Generic;

namespace CSharpBlockchain
{
    public class TupleList<T1, T2> : List<Tuple<T1, T2>>
    {
        public void Add(T1 item1, T2 item2)
        {
            Add(new Tuple<T1, T2>(item1, item2));
        }

        public Tuple<IEnumerable<T1>, IEnumerable<T2>> Split()
        {
            List<T1> list1 = new List<T1>();
            List<T2> list2 = new List<T2>();
            foreach (Tuple<T1, T2> t in this)
            {
                list1.Add(t.Item1);
                list2.Add(t.Item2);
            }

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>>(list1, list2);
        }
    }
}
