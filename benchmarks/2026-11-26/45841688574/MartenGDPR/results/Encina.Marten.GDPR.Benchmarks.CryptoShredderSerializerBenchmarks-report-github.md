```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    354.6 ns |    126.7 ns |   6.95 ns |  1.00 |    0.02 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    541.7 ns |    287.2 ns |  15.74 ns |  1.53 |    0.05 |    2 | 0.0563 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 17,189.4 ns |  2,545.5 ns | 139.53 ns | 48.49 |    0.89 |    4 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 23,397.3 ns | 12,717.9 ns | 697.11 ns | 66.00 |    2.04 |    5 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  2,911.4 ns |    523.9 ns |  28.72 ns |  8.21 |    0.16 |    3 | 0.2174 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  3,471.3 ns |    177.8 ns |   9.74 ns |  9.79 |    0.17 |    3 | 0.1335 |    2288 B |        8.94 |
