namespace VectorSearch.Common.Vectors
{
    public class SparseVectorStore : IDisposable
    {
        private readonly Stream stream;
        private readonly BinaryWriter writer;
        private readonly BinaryReader reader;
        private readonly ReaderWriterLockSlim lck;

        private int documentCount = 0;

        public SparseVectorStore(
            string path)
        {
            stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            writer = new BinaryWriter(stream);
            reader = new BinaryReader(stream);

            lck = new ReaderWriterLockSlim();

            if (stream.Length == 0)
            {
                stream.Seek(0, SeekOrigin.Begin);
                writer.Write(documentCount);
            }
            else
            {
                stream.Seek(0, SeekOrigin.Begin);
                documentCount = reader.ReadInt32();
            }
        }

        public void Dispose()
        {
            writer.Close();
            stream.Close();
            writer.Dispose();
            stream.Dispose();
        }

        public void Add(int documentId, SparseVector vector)
        {
            try 
            {
                this.lck.EnterWriteLock();

                stream.Seek(0, SeekOrigin.End);
                writer.Write(documentId);
                writer.Write(vector.Length());
                foreach (var kv in vector)
                {
                    writer.Write(kv.Key);
                    writer.Write(kv.Value);
                }
                stream.Seek(0, SeekOrigin.Begin);
                writer.Write(documentCount++);
            }
            finally
            {
                this.lck.ExitWriteLock();
            }
        }

        public IEnumerable<SparseVectorRecord> Records()
        {
            try
            {
                this.lck.EnterReadLock();

                stream.Seek(4, SeekOrigin.Begin);

                var current = 0;
                while (current < documentCount)
                {
                    var documentId = reader.ReadInt32();
                    var dimensionCount = reader.ReadInt32();
                    var k = new Dictionary<int, double>();
                    for (var d = 0; d < dimensionCount; d++)
                    {
                        var offset = reader.ReadInt32();
                        var value = reader.ReadDouble();
                        k.Add(offset, value);
                    }

                    yield return new SparseVectorRecord(documentId, new SparseVector(k));

                    current++;
                }
            }
            finally
            {
                this.lck.ExitReadLock();
            }
        }
    }

    public record SparseVectorRecord(int DocumentId, SparseVector Vector);
}
