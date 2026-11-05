```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.74GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    595.6 ns |    67.18 ns |   3.68 ns |  1.00 |    0.01 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    940.0 ns |    99.73 ns |   5.47 ns |  1.58 |    0.01 |    2 | 0.0563 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 29,348.7 ns | 1,579.25 ns |  86.56 ns | 49.28 |    0.29 |    4 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 38,555.1 ns | 4,788.83 ns | 262.49 ns | 64.74 |    0.52 |    5 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,407.4 ns |   210.61 ns |  11.54 ns |  9.08 |    0.05 |    3 | 0.2136 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  6,444.9 ns |   431.43 ns |  23.65 ns | 10.82 |    0.07 |    3 | 0.1297 |    2288 B |        8.94 |
