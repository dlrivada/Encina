```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.49GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    642.7 ns |    73.70 ns |   4.04 ns |  1.00 |    0.01 |    1 | 0.0095 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |  1,011.3 ns |   182.74 ns |  10.02 ns |  1.57 |    0.02 |    2 | 0.0362 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 32,702.1 ns | 1,903.27 ns | 104.32 ns | 50.88 |    0.31 |    5 | 0.3662 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 39,211.2 ns |   468.78 ns |  25.70 ns | 61.01 |    0.33 |    5 | 0.4272 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,447.1 ns |   342.64 ns |  18.78 ns |  8.48 |    0.05 |    3 | 0.1450 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  6,925.1 ns |   452.88 ns |  24.82 ns | 10.78 |    0.07 |    4 | 0.0839 |    2288 B |        8.94 |
