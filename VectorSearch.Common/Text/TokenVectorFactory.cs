using VectorSearch.Common.Vectors;

namespace VectorSearch.Common.Text
{
    public class TokenVectorFactory 
    {
        private readonly Dictionary<string, (int Offset, double Count)> indexMap;
        private double documentCount = 0;

        public TokenVectorFactory()
        {
            this.indexMap = new Dictionary<string, (int Offset, double Count)>();
        }

        public TokenVectorFactory(
            Dictionary<string, (int Offset, double Count)> indexMap, 
            double documentCount)
        {
            this.indexMap = indexMap;
            this.documentCount = documentCount;
        }   

        public SparseVector ToVector(IEnumerable<string> tokens)
        {
            var vector = new SparseVector();

            foreach (var token in tokens.GroupBy(S => S.ToLower())
                .ToDictionary(K => K.Key, V => V.Count()))
            {
                if (indexMap.ContainsKey(token.Key))
                {
                    var ii = indexMap[token.Key];
                    var tf = (double)token.Value / (double)tokens.Count();
                    var idf = Math.Log(documentCount / ii.Count);
                    var tf_idf = idf * tf;
                    vector[ii.Offset] = tf_idf; 
                }
            }

            return vector;
        }

        public void AddDocument(IEnumerable<string> tokens)
        {
            foreach (var token in tokens.ToHashSet())
            {
                if (!this.indexMap.ContainsKey(token))
                {
                    this.indexMap.Add(token, (this.indexMap.Count, 1));
                }
                else
                {
                    var t = this.indexMap[token];
                    t.Count++;
                    this.indexMap[token] = t;
                }
            }

            this.documentCount++;
        }

        public string FromOffset(int offset)
        {
            return this.indexMap.Keys.ElementAt(offset);
        }

        public int Length()
        {
            return this.indexMap.Count;
        }

        public void Save(Stream stream)
        {
            using (stream)
            {
                using (var writer = new BinaryWriter(stream))
                {
                    this.Save(writer);
                }
            }
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(this.documentCount);
            writer.Write(this.indexMap.Count);
            foreach (var kv in this.indexMap)
            {
                writer.Write(kv.Key);
                writer.Write(kv.Value.Offset);
                writer.Write(kv.Value.Count);
            }
        }

        public static TokenVectorFactory Load(Stream stream)
        {
            using (stream)
            {
                using (var reader = new BinaryReader(stream))
                {
                    return Load(reader);
                }
            }
        }

        public static TokenVectorFactory Load(BinaryReader reader)
        {
            var documentCount = reader.ReadDouble();
            var indexMapCount = reader.ReadInt32();
            var imc = new Dictionary<string, (int Offset, double Count)>();
            for (var x = 0; x < indexMapCount; x++)
            {
                var key = reader.ReadString();
                var offset = reader.ReadInt32();
                var count = reader.ReadDouble();

                imc.Add(key, (offset, count));
            }

            return new TokenVectorFactory(imc, documentCount);
        }
    }
}
