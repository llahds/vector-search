namespace VectorSearch.Common.Vectors
{
    public class SparseVectorIndexReader
    {
        private readonly Dictionary<int, Range[]> dimensions;
        private readonly Dictionary<int, double> norms;

        public SparseVectorIndexReader(
            Dictionary<int, Range[]> dimensions,
            Dictionary<int, double> norms)
        {
            this.dimensions = dimensions;
            this.norms = norms;
        }

        public static SparseVectorIndexReader Load(Stream stream)
        {
            using (stream)
            {
                return Load(new BinaryReader(stream));
            }
        }

        public static SparseVectorIndexReader Load(BinaryReader reader)
        {
            var dimensionCount = reader.ReadInt32();
            var dimensions = new Dictionary<int, Range[]>();

            for (var d = 0; d < dimensionCount; d++)
            {
                var dimensionOffset = reader.ReadInt32();
                var rangeLength = reader.ReadInt32();
                dimensions.Add(dimensionOffset, new Range[rangeLength]);
                var dimension = dimensions[dimensionOffset];
                for (var r = 0; r < rangeLength; r++)
                {
                    var range = new Range();
                    range.Value = reader.ReadDouble();
                    var documentCount = reader.ReadInt32();
                    range.DocumentIds = new int[documentCount];
                    for (var dc = 0; dc < documentCount; dc++)
                    {
                        range.DocumentIds[dc] = reader.ReadInt32();
                    }
                    dimension[r] = range;
                }
            }

            var normsCount = reader.ReadInt32();
            var norms = new Dictionary<int, double>(normsCount);
            
            for (var n = 0; n < normsCount; n++)
            {
                norms.Add(reader.ReadInt32(), reader.ReadDouble());
            }

            return new SparseVectorIndexReader(dimensions, norms);
        }

        public Result[] Query(SparseVectorQuery query)
        {
            var mostLikely = new Dictionary<int, ScanScore[]>();

            foreach (var kv in query.Query.OrderByDescending(KV => KV.Value).Take(query.MaxScanDimensions))
            {
                if (this.dimensions.ContainsKey(kv.Key))
                {
                    var ranges = this.dimensions[kv.Key];

                    var r = new List<ScanScore>();
                    foreach (var range in ranges)
                    {
                        r.Add(new ScanScore { Range = range, Score = Math.Abs(kv.Value - range.Value) });
                    }

                    if (r.Count > 0)
                    {
                        r.Sort(new Comparison<ScanScore>((L, R) => L.Score.CompareTo(R.Score) * -1));
                        foreach (var b in r.Take(query.MaxScanNodes))
                        {
                            b.Score = b.Range.Value * kv.Value;
                        }
                        mostLikely.Add(kv.Key, r.Take(query.MaxScanNodes).ToArray());
                    }
                }
            }

            var scores = new Dictionary<int, double>();

            foreach (var d in mostLikely)
            {
                foreach (var r in d.Value)
                {
                    foreach (var id in r.Range.DocumentIds)
                    {
                        if (scores.TryAdd(id, r.Score) == false)
                        {
                            scores[id] += r.Score;
                        }
                    }
                }
            }

            var norm = query.Query.Norm();

            return scores
                .OrderByDescending(S => S.Value)
                .Take(query.TopN)
                .Select(S => new Result(S.Key, S.Value / (norm * this.norms[S.Key])))
                .ToArray();
        }

        public struct Range 
        {
            public double Value { get; set; }
            public int[] DocumentIds { get; set; }
        }

        public class ScanScore
        {
            public Range Range { get; set; }

            public double Score { get; set; }
        }
    }
}
