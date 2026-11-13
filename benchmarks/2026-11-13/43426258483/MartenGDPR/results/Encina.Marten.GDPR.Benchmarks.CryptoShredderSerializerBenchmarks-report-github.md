```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    670.5 ns |    77.83 ns |  4.27 ns |  1.00 |    0.01 |    1 | 0.0095 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    969.8 ns |    47.19 ns |  2.59 ns |  1.45 |    0.01 |    2 | 0.0362 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 32,439.4 ns | 1,140.20 ns | 62.50 ns | 48.38 |    0.28 |    5 | 0.3662 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 39,109.3 ns |   665.09 ns | 36.46 ns | 58.33 |    0.33 |    6 | 0.4272 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,329.4 ns |   309.75 ns | 16.98 ns |  7.95 |    0.05 |    3 | 0.1450 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  6,927.5 ns |   206.57 ns | 11.32 ns | 10.33 |    0.06 |    4 | 0.0839 |    2288 B |        8.94 |
