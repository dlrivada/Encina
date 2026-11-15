```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                            | Mean        | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    355.5 ns |   4.16 ns |   6.10 ns |  1.00 |    0.02 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    570.3 ns |   6.90 ns |   9.90 ns |  1.60 |    0.04 |    2 | 0.0563 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 17,644.3 ns | 481.46 ns | 705.71 ns | 49.64 |    2.12 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 22,549.2 ns | 330.89 ns | 452.93 ns | 63.44 |    1.65 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  2,831.6 ns |  76.24 ns | 111.76 ns |  7.97 |    0.34 |    3 | 0.2174 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  3,454.4 ns |  65.05 ns |  93.29 ns |  9.72 |    0.31 |    4 | 0.1335 |    2288 B |        8.94 |
