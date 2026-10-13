```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|-------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    369.0 ns |    296.06 ns |  16.23 ns |  1.00 |    0.05 |    1 | 0.0029 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    639.1 ns |      3.90 ns |   0.21 ns |  1.73 |    0.06 |    2 | 0.0105 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 21,475.1 ns | 15,041.38 ns | 824.47 ns | 58.27 |    2.91 |    5 | 0.1221 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 27,253.9 ns |  1,481.27 ns |  81.19 ns | 73.94 |    2.75 |    6 | 0.1221 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  3,512.2 ns |  1,242.45 ns |  68.10 ns |  9.53 |    0.39 |    3 | 0.0420 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  4,947.7 ns |  2,189.96 ns | 120.04 ns | 13.42 |    0.57 |    4 | 0.0229 |    2288 B |        8.94 |
