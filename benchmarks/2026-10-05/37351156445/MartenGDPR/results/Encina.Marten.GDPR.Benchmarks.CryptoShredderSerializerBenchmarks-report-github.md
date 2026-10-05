```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    345.9 ns |    351.2 ns |  19.25 ns |  1.00 |    0.07 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    517.6 ns |    231.4 ns |  12.68 ns |  1.50 |    0.08 |    2 | 0.0563 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 17,120.4 ns |    524.1 ns |  28.73 ns | 49.60 |    2.32 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 27,146.7 ns | 14,941.2 ns | 818.98 ns | 78.65 |    4.21 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  2,886.8 ns |  1,566.5 ns |  85.87 ns |  8.36 |    0.45 |    3 | 0.2174 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  3,790.0 ns |    402.5 ns |  22.06 ns | 10.98 |    0.52 |    4 | 0.1297 |    2288 B |        8.94 |
