```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    627.3 ns |    64.21 ns |   3.52 ns |  1.00 |    0.01 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    993.7 ns |   128.75 ns |   7.06 ns |  1.58 |    0.01 |    2 | 0.0553 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 34,902.8 ns | 1,563.15 ns |  85.68 ns | 55.64 |    0.29 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 45,569.6 ns | 4,210.34 ns | 230.78 ns | 72.64 |    0.47 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,518.8 ns |   102.50 ns |   5.62 ns |  8.80 |    0.04 |    3 | 0.2136 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  7,830.0 ns |   631.61 ns |  34.62 ns | 12.48 |    0.08 |    4 | 0.1221 |    2288 B |        8.94 |
