using System.Collections;

namespace VectorSearch.Common.Vectors
{
    public class SparseVector : IEnumerable<KeyValuePair<int, double>>
    {
        private readonly Dictionary<int, double> values;
        private double? norm;

        public SparseVector()
        {
            values = new Dictionary<int, double>();
        
        }

        public SparseVector(Dictionary<int, double> values)
        {
            this.values = values;
        }

        public double this[int index]
        {
            get
            {
                if (values.ContainsKey(index))
                {
                    return values[index];
                }
                else
                {
                    return 0.0;
                }
            }
            set
            {
                norm = null;

                if (!values.ContainsKey(index))
                {
                    values.Add(index, value);
                }
                else
                {
                    values[index] = value;
                }
            }
        }

        public int Length()
        {
            return values.Count;
        }

        public double Similarity(SparseVector vector)
        {
            var c = Inner(vector) / (Norm() * vector.Norm());
            if (double.IsNaN(c))
            {
                c = 0.0;
            }
            return c;
        }

        public double Inner(SparseVector vector)
        {
            var shortest = values.Where(V => vector.values.ContainsKey(V.Key)).ToArray();
            var r = 0.0;
            foreach (var kv in shortest)
            {
                r += kv.Value * vector[kv.Key];
            }
            return r;
        }

        public double Norm()
        {
            if (norm.HasValue)
            {
                return norm.Value;
            }

            var r = 0.0;
            foreach (var kv in values)
            {
                r += Math.Pow(kv.Value, 2);
            }
            r = Math.Pow(r, 0.5);
            norm = r;
            return r;
        }

        public SparseVector Average(SparseVector vector)
        {
            var shortest = values.Keys.Concat(vector.values.Keys).ToHashSet();
            var n = new SparseVector();
            foreach (var kv in shortest)
            {
                n[kv] = (this[kv] + vector[kv]) / 2.0;
            }
            return n;
        }

        public IEnumerator<KeyValuePair<int, double>> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
