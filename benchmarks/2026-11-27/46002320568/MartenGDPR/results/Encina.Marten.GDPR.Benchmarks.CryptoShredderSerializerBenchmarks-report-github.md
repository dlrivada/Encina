```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.58GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    380.1 ns |  85.43 ns |  4.68 ns |  1.00 |    0.02 |    1 | 0.0029 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    602.5 ns | 252.90 ns | 13.86 ns |  1.59 |    0.04 |    2 | 0.0105 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 18,718.0 ns | 153.00 ns |  8.39 ns | 49.25 |    0.52 |    4 | 0.1221 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 21,242.3 ns | 367.23 ns | 20.13 ns | 55.89 |    0.59 |    4 | 0.1221 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  3,598.3 ns | 578.38 ns | 31.70 ns |  9.47 |    0.12 |    3 | 0.0420 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  3,772.4 ns | 126.32 ns |  6.92 ns |  9.92 |    0.11 |    3 | 0.0267 |    2288 B |        8.94 |
