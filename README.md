# Sparse Vector Search using Cosine Similarity

This is a simple data structure for storing and retrieving sparse vectors using an approximate cosine similarity. It's not meant to be 
as performant as tools like PySparNN but more of a thought experiment to see how much performance can be gained through straight-forward
optimization and C#.

```
// load the index into RAM
var reader = SparseVectorIndexReader.Load(File.OpenRead(Path.Combine(path, "reader.dat")));

// create a sparse vector and set the values
var v = new SparseVector();
v[0] = 1.0;
v[1] = 1.0;
v[10998] = 1.0;

// query the index
var s0 = reader.Query(new SparseVectorQuery { Query = v.Vector });
```

![vector-search](https://user-images.githubusercontent.com/49455496/194170044-42762257-c365-481f-9393-bd3a98abf7f8.gif)

The above results are from the enron email dataset (517K documents). If you want to run this locally you'll need to download the z7 file 
that contains the index (vectors.dat) and the enron vectors (enron-vectors.dat) store file. 

https://drive.google.com/file/d/1SNT8NIpPTAgfV_lx1JdMoWsmV_Yhup7g/view?usp=sharing

Unzip the folder locally and change "<path to extracted 7z file>" to the correct path in Program.cs.

<img width="829" alt="image" src="https://user-images.githubusercontent.com/49455496/194174886-396fd36a-8b84-4605-9b98-77742680c1ea.png">

## How does it work?

The sparse vectors are stored in an inverted index. The index is keyed by the vector's offsets. Each entry in the inverted index has N bins. Each bin
has a value and an array of ids. 

So vectors that looks like: 

```
vector 1 = [ { 2, .456 }, { 5663, .998 } ]; 
vector 2 = [ { 3, .044 }, { 5663, .234 } ];
``` 

are deconstructed into:

```
offset 2 = [ { value: .456, vectors: [ 1 ] } ];
offset 3 = [ { value: .044, vectors: [ 2 ] } ];
offset 5663: [ { value: .382, vectors: [ 1, 2 ] } 
```

Querying the inverse index is done in three stages:

* Find bins that are near the query vector and use the bin's value to approximate the similarity 
* Iterate through each bin's ids and sum the total
* Find the ids with the highest dot product and look up the id's norm to finish the cosine calculation


