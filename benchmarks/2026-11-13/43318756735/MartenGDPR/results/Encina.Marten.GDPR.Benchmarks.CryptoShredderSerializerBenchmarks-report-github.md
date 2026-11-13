```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.04GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    393.8 ns |    46.60 ns |   2.55 ns |  1.00 |    0.01 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    622.4 ns |    72.93 ns |   4.00 ns |  1.58 |    0.01 |    2 | 0.0563 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 18,623.2 ns | 1,543.59 ns |  84.61 ns | 47.29 |    0.32 |    4 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 25,649.5 ns | 3,211.21 ns | 176.02 ns | 65.13 |    0.53 |    5 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  3,293.0 ns |   991.44 ns |  54.34 ns |  8.36 |    0.13 |    3 | 0.2174 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  3,902.0 ns |   537.71 ns |  29.47 ns |  9.91 |    0.09 |    3 | 0.1297 |    2288 B |        8.94 |
