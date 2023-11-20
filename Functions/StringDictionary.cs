using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EditorManagement.Functions
{
    public class StringDictionary<T> : IEnumerable
    {
        public StringDictionary()
        {

        }

        public T this[string at]
        {
            get
            {
                if (ContainsKey(at))
                    return Values[Keys.FindIndex(x => x == at)];
                throw new IndexOutOfRangeException("Key was not present in dictionary");
            }
        }

        public List<string> Keys { get; private set; } = new List<string>();
        public List<T> Values { get; private set; } = new List<T>();

        public int Count => Keys.Count;

        public void Add(string key, T value)
        {
            if (!ContainsKey(key))
            {
                Keys.Add(key);
                Values.Add(value);
            }
        }

        public bool ContainsKey(string key) => Keys.Contains(key);

        public T ElementAt(int i) => Values[i];

        public void Clear()
        {
            Keys.Clear();
            Values.Clear();
        }

        public bool TryGetValue(string key, out T value)
        {
            if (ContainsKey(key))
            {
                value = this[key];
                return true;
            }

            value = default;
            return false;
        }

        public void FindValue(Predicate<T> predicate) => Values.Find(predicate);

        public IEnumerable<I> Cast<I>() => Values.Cast<I>();

        public IEnumerator GetEnumerator() => new Enumerator(this, 2);

        public int version;

        [Serializable]
        public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
        {
            internal Enumerator(StringDictionary<T> stringDictionary, int getEnumeratorRetType)
            {
                this.stringDictionary = stringDictionary;
                this.getEnumeratorRetType = getEnumeratorRetType;
                index = 0;
                current = default;
            }

            public T Current => current;

            object IEnumerator.Current => current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                while (this.index < this.stringDictionary.Count)
                {
                    if (this.stringDictionary.ElementAt(index).GetHashCode() >= 0)
                    {
                        this.current = stringDictionary.ElementAt(index);
                        this.index++;
                        return true;
                    }
                    this.index++;
                }
                this.index = this.stringDictionary.Count + 1;
                this.current = default;
                return false;
            }

            public void Reset()
            {
                index = 0;
                current = default;
            }

            StringDictionary<T> stringDictionary;
            int index;
            int getEnumeratorRetType;
            T current;
        }
    }
}
