using System.Diagnostics;
using VectorSearch.Common.Vectors;

var path = @"<path to extracted 7z file>";

using (var store = new SparseVectorStore(Path.Combine(path, "enron-vectors.dat")))
{
    var reader = SparseVectorIndexReader.Load(File.OpenRead(Path.Combine(path, "reader.dat")));

    var stopwatch = new Stopwatch();

    foreach (var v in store.Records().Take(100))
    {
        stopwatch.Restart();
        var s0 = reader.Query(new SparseVectorQuery { Query = v.Vector });
        stopwatch.Stop();        
        Console.WriteLine($"Actual: {v.DocumentId}, Guess: {s0[0].documentId}, Score: {s0[0].score}, Time: {stopwatch.ElapsedMilliseconds}ms");
    }
}