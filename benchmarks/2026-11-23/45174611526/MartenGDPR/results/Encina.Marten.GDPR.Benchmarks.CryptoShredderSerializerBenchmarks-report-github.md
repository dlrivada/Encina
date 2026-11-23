```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    500.8 ns |    14.17 ns |   0.78 ns |  1.00 |    0.00 |    1 | 0.0029 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    774.0 ns |   154.76 ns |   8.48 ns |  1.55 |    0.01 |    2 | 0.0105 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 26,461.4 ns | 1,083.35 ns |  59.38 ns | 52.84 |    0.12 |    4 | 0.1221 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 31,395.3 ns | 3,949.59 ns | 216.49 ns | 62.69 |    0.38 |    4 | 0.1221 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  4,704.8 ns |   209.14 ns |  11.46 ns |  9.39 |    0.02 |    3 | 0.0381 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  5,217.0 ns |   268.55 ns |  14.72 ns | 10.42 |    0.03 |    3 | 0.0229 |    2288 B |        8.94 |
