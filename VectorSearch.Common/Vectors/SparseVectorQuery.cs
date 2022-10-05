namespace VectorSearch.Common.Vectors
{
    public class SparseVectorQuery
    {
        public SparseVector Query { get; set; }
        public int MaxScanDimensions { get; set; } = 100;
        public int MaxScanNodes { get; set; } = 100;
        public int TopN { get; set; } = 100;
    }
}
