```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    574.9 ns |    51.82 ns |   2.84 ns |  1.00 |    0.01 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    917.7 ns |    35.80 ns |   1.96 ns |  1.60 |    0.01 |    2 | 0.0563 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 30,381.7 ns | 1,718.62 ns |  94.20 ns | 52.85 |    0.27 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 46,620.9 ns | 4,237.29 ns | 232.26 ns | 81.09 |    0.49 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,000.1 ns |   592.90 ns |  32.50 ns |  8.70 |    0.06 |    3 | 0.2136 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  7,161.0 ns |   469.77 ns |  25.75 ns | 12.46 |    0.07 |    4 | 0.1297 |    2288 B |        8.94 |
