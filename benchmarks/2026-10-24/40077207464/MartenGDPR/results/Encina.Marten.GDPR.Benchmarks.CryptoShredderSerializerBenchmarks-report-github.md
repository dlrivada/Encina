```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    623.6 ns |    20.39 ns |   1.12 ns |  1.00 |    0.00 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    988.8 ns |    33.51 ns |   1.84 ns |  1.59 |    0.00 |    2 | 0.0553 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 32,874.4 ns | 1,945.38 ns | 106.63 ns | 52.72 |    0.17 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 46,188.3 ns | 5,974.47 ns | 327.48 ns | 74.07 |    0.47 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,400.3 ns |    59.77 ns |   3.28 ns |  8.66 |    0.01 |    3 | 0.2136 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  7,173.9 ns |   248.69 ns |  13.63 ns | 11.50 |    0.03 |    4 | 0.1297 |    2288 B |        8.94 |
