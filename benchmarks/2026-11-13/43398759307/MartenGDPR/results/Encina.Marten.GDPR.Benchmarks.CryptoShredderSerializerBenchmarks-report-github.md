```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    632.4 ns |    62.34 ns |   3.42 ns |  1.00 |    0.01 |    1 | 0.0095 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    978.7 ns |    95.16 ns |   5.22 ns |  1.55 |    0.01 |    2 | 0.0362 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 32,453.3 ns |   271.47 ns |  14.88 ns | 51.32 |    0.24 |    5 | 0.3662 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 38,818.1 ns | 4,727.44 ns | 259.13 ns | 61.38 |    0.46 |    5 | 0.4272 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,278.2 ns |   374.18 ns |  20.51 ns |  8.35 |    0.05 |    3 | 0.1450 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  6,901.4 ns |   534.91 ns |  29.32 ns | 10.91 |    0.06 |    4 | 0.0839 |    2288 B |        8.94 |
