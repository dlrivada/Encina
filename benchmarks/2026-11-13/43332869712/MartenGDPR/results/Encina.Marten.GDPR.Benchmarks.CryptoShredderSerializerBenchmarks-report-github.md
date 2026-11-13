```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    610.2 ns |    19.19 ns |   1.05 ns |  1.00 |    0.00 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    965.8 ns |    29.30 ns |   1.61 ns |  1.58 |    0.00 |    2 | 0.0553 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 30,022.6 ns | 4,268.66 ns | 233.98 ns | 49.20 |    0.34 |    4 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 38,155.6 ns | 5,484.85 ns | 300.64 ns | 62.53 |    0.44 |    5 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,470.7 ns |   499.88 ns |  27.40 ns |  8.96 |    0.04 |    3 | 0.2136 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  6,170.8 ns |    67.83 ns |   3.72 ns | 10.11 |    0.02 |    3 | 0.1297 |    2288 B |        8.94 |
