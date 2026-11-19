```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.01GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    609.6 ns |    62.55 ns |   3.43 ns |  1.00 |    0.01 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    965.5 ns |    57.72 ns |   3.16 ns |  1.58 |    0.01 |    2 | 0.0553 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 32,264.8 ns | 8,261.66 ns | 452.85 ns | 52.93 |    0.69 |    4 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 38,580.6 ns | 8,604.20 ns | 471.63 ns | 63.29 |    0.74 |    4 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,509.8 ns |   773.29 ns |  42.39 ns |  9.04 |    0.07 |    3 | 0.2136 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  6,471.2 ns |   452.11 ns |  24.78 ns | 10.62 |    0.06 |    3 | 0.1297 |    2288 B |        8.94 |
