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
| InnerSerializer_NonPii            |    683.4 ns |    48.31 ns |   2.65 ns |  1.00 |    0.00 |    1 | 0.0095 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |  1,012.1 ns |    12.70 ns |   0.70 ns |  1.48 |    0.01 |    2 | 0.0362 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 32,387.6 ns | 2,364.46 ns | 129.60 ns | 47.39 |    0.23 |    5 | 0.3662 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 38,631.3 ns | 1,061.38 ns |  58.18 ns | 56.53 |    0.20 |    5 | 0.4272 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,575.7 ns |   161.83 ns |   8.87 ns |  8.16 |    0.03 |    3 | 0.1450 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  6,749.6 ns |   706.97 ns |  38.75 ns |  9.88 |    0.06 |    4 | 0.0839 |    2288 B |        8.94 |
