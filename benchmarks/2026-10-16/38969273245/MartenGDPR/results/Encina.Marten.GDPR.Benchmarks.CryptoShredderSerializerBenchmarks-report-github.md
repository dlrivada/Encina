```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 4.14GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|-------------:|------------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    339.8 ns |     91.59 ns |     5.02 ns |  1.00 |    0.02 |    1 | 0.0029 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    524.0 ns |     83.72 ns |     4.59 ns |  1.54 |    0.02 |    2 | 0.0105 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 20,043.9 ns |  1,800.06 ns |    98.67 ns | 58.99 |    0.79 |    5 | 0.1221 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 27,195.7 ns | 19,313.54 ns | 1,058.64 ns | 80.04 |    2.88 |    6 | 0.1221 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  3,308.6 ns |    601.00 ns |    32.94 ns |  9.74 |    0.15 |    3 | 0.0420 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  4,158.2 ns |    322.88 ns |    17.70 ns | 12.24 |    0.16 |    4 | 0.0229 |    2288 B |        8.94 |
