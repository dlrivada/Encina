```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|-------------:|------------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    627.9 ns |     81.22 ns |     4.45 ns |  1.00 |    0.01 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    950.3 ns |     97.47 ns |     5.34 ns |  1.51 |    0.01 |    2 | 0.0553 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 31,541.9 ns |    766.99 ns |    42.04 ns | 50.23 |    0.31 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 45,189.0 ns | 20,972.87 ns | 1,149.59 ns | 71.97 |    1.65 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,730.3 ns |    800.89 ns |    43.90 ns |  9.13 |    0.08 |    3 | 0.2136 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  7,111.2 ns |    369.32 ns |    20.24 ns | 11.33 |    0.07 |    4 | 0.1297 |    2288 B |        8.94 |
