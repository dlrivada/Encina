```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    619.3 ns |    62.13 ns |   3.41 ns |  1.00 |    0.01 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    962.2 ns |    53.37 ns |   2.93 ns |  1.55 |    0.01 |    2 | 0.0553 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 32,136.9 ns | 4,031.87 ns | 221.00 ns | 51.89 |    0.40 |    4 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 38,318.0 ns | 2,300.81 ns | 126.12 ns | 61.87 |    0.34 |    4 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,477.2 ns |   221.90 ns |  12.16 ns |  8.84 |    0.05 |    3 | 0.2136 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  6,426.2 ns |   705.73 ns |  38.68 ns | 10.38 |    0.07 |    3 | 0.1297 |    2288 B |        8.94 |
