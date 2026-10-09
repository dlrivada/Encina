```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    449.7 ns |   233.05 ns |  12.77 ns |  1.00 |    0.04 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    675.4 ns |    38.40 ns |   2.10 ns |  1.50 |    0.04 |    2 | 0.0563 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 23,072.4 ns | 1,508.21 ns |  82.67 ns | 51.33 |    1.29 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 36,577.5 ns | 2,380.59 ns | 130.49 ns | 81.37 |    2.04 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  3,861.0 ns |    96.52 ns |   5.29 ns |  8.59 |    0.21 |    3 | 0.2136 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  5,409.8 ns |    73.53 ns |   4.03 ns | 12.04 |    0.30 |    4 | 0.1297 |    2288 B |        8.94 |
