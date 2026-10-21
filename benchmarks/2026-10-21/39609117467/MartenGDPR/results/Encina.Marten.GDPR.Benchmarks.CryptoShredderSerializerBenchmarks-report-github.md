```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    613.6 ns |    10.03 ns |   0.55 ns |  1.00 |    0.00 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    967.8 ns |    61.46 ns |   3.37 ns |  1.58 |    0.00 |    2 | 0.0553 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 32,126.2 ns | 3,915.17 ns | 214.60 ns | 52.36 |    0.31 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 45,858.9 ns | 3,099.20 ns | 169.88 ns | 74.74 |    0.25 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,498.8 ns |   364.45 ns |  19.98 ns |  8.96 |    0.03 |    3 | 0.2136 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  7,242.4 ns |   442.06 ns |  24.23 ns | 11.80 |    0.04 |    4 | 0.1297 |    2288 B |        8.94 |
