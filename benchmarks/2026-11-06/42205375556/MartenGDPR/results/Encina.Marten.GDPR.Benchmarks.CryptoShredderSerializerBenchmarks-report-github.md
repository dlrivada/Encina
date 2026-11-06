```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    458.3 ns |    20.65 ns |   1.13 ns |  1.00 |    0.00 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    718.0 ns |    27.63 ns |   1.51 ns |  1.57 |    0.00 |    2 | 0.0563 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 21,305.4 ns | 2,451.82 ns | 134.39 ns | 46.49 |    0.27 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 30,576.8 ns | 6,719.42 ns | 368.31 ns | 66.72 |    0.71 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  4,161.1 ns |   122.04 ns |   6.69 ns |  9.08 |    0.02 |    3 | 0.2136 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  5,110.0 ns |   124.96 ns |   6.85 ns | 11.15 |    0.03 |    4 | 0.1297 |    2288 B |        8.94 |
