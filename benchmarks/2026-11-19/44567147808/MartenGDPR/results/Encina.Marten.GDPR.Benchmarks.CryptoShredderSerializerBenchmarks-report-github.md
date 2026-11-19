```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.28GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|-----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    374.1 ns |   153.9 ns |   8.43 ns |  1.00 |    0.03 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    566.5 ns |   116.5 ns |   6.39 ns |  1.51 |    0.03 |    2 | 0.0563 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 17,519.5 ns | 2,049.4 ns | 112.33 ns | 46.85 |    0.94 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 23,456.6 ns | 2,670.0 ns | 146.35 ns | 62.73 |    1.26 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  2,889.2 ns |   667.1 ns |  36.57 ns |  7.73 |    0.17 |    3 | 0.2174 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  3,592.6 ns |   685.1 ns |  37.55 ns |  9.61 |    0.21 |    4 | 0.1335 |    2288 B |        8.94 |
