namespace VectorSearch.Common.Vectors
{
    public class SparseVectorIndexWriter
    {
        private readonly Dictionary<int, double> norms = new Dictionary<int, double>();
        private readonly Dictionary<int, List<VectorItem>> dimensions = new Dictionary<int, List<VectorItem>>();
        private readonly int bucketSize;

        public SparseVectorIndexWriter(int bucketSize)
        {
            this.bucketSize = bucketSize;
        }

        public void Add(int documentId, SparseVector vector)
        {
            if (this.norms.ContainsKey(documentId) == false)
            {
                this.norms.Add(documentId, vector.Norm());
            } 
            else
            {
                this.norms[documentId] = vector.Norm();
            }
            

            foreach (var kv in vector)
            {
                if (this.dimensions.ContainsKey(kv.Key) == false)
                {
                    this.dimensions.Add(kv.Key, new List<VectorItem>());
                }

                this.dimensions[kv.Key].Add(new VectorItem(documentId, kv.Value));
            }
        }

        public void WriteTo(Stream stream)
        {
            using (stream)
            {
                this.WriteTo(new BinaryWriter(stream));
            }
        }

        public void WriteTo(BinaryWriter writer)
        {
            //var d = new Dictionary<int, Range[]>();

            var dimensionCount = 0;
            // allocate 4 bytes at the front of the index for the 
            // number of dimensions
            writer.Write(dimensionCount);

            foreach (var dimension in this.dimensions)
            {
                var ranges = GetRanges(dimension.Value, bucketSize);

                if (ranges.Length > 0)
                {
                    writer.Write(dimension.Key);
                    writer.Write(ranges.Length);

                    foreach (var range in ranges)
                    {
                        writer.Write(range.Value);

                        writer.Write(range.DocumentIds.Count);
                        foreach (var documentId in range.DocumentIds)
                        {
                            writer.Write(documentId);
                        }
                    }

                    dimensionCount++;
                }
            }

            writer.Write(this.norms.Count);
            foreach (var norm in this.norms)
            {
                writer.Write(norm.Key);
                writer.Write(norm.Value);
            }

            writer.BaseStream.Seek(0, SeekOrigin.Begin);
            writer.Write(dimensionCount);
        }

        private Range[] GetRanges(IEnumerable<VectorItem> source, int totalBuckets)
        {
            var min = source.Min(S => S.value);
            var max = source.Max(S => S.value);
            var buckets = new Range[totalBuckets];
            var bucketSize = (max - min) / totalBuckets;

            for (int i = 0; i < totalBuckets; i++)
            {
                buckets[i] = new Range()
                {
                    DocumentIds = new List<int>(),
                    Value = bucketSize * (i + 1)
                };
            }
            
            foreach (var item in source)
            {
                int bucketIndex = 0;
                if (bucketSize > 0.0)
                {
                    bucketIndex = (int)((item.value - min) / bucketSize);
                    if (bucketIndex == totalBuckets)
                    {
                        bucketIndex--;
                    }
                }
                buckets[bucketIndex].DocumentIds.Add(item.documentId);
            }

            return buckets.Where(B => B.DocumentIds.Count > 0 && B.Value > 0).ToArray();
        }

        public class Range
        {
            public double Value { get; set; }
            public List<int> DocumentIds { get; set; }
        }
    }
}
