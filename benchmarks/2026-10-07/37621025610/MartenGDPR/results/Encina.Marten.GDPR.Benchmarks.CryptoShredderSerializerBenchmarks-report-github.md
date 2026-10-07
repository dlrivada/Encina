```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|-------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    363.8 ns |    208.04 ns |  11.40 ns |  1.00 |    0.04 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    532.3 ns |     14.98 ns |   0.82 ns |  1.46 |    0.04 |    2 | 0.0563 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 18,198.2 ns | 10,756.22 ns | 589.58 ns | 50.05 |    1.96 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 26,733.9 ns |  3,838.30 ns | 210.39 ns | 73.53 |    2.08 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  3,008.1 ns |    122.55 ns |   6.72 ns |  8.27 |    0.23 |    3 | 0.2174 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  3,989.6 ns |    561.56 ns |  30.78 ns | 10.97 |    0.31 |    4 | 0.1297 |    2288 B |        8.94 |
