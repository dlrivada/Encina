```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                            | Mean        | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    350.9 ns |   4.04 ns |   5.66 ns |  1.00 |    0.02 |    1 | 0.0029 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    613.9 ns |  13.62 ns |  19.09 ns |  1.75 |    0.06 |    2 | 0.0105 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 20,307.5 ns | 167.98 ns | 224.25 ns | 57.88 |    1.09 |    5 | 0.1221 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 27,296.7 ns | 495.57 ns | 694.72 ns | 77.81 |    2.29 |    6 | 0.1221 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  3,436.1 ns |  27.35 ns |  36.52 ns |  9.79 |    0.18 |    3 | 0.0420 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  4,419.8 ns | 122.77 ns | 179.96 ns | 12.60 |    0.54 |    4 | 0.0229 |    2288 B |        8.94 |
