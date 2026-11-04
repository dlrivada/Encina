```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.54GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    347.7 ns |   111.84 ns |   6.13 ns |  1.00 |    0.02 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    513.1 ns |   165.20 ns |   9.06 ns |  1.48 |    0.03 |    2 | 0.0563 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 15,878.6 ns |   483.55 ns |  26.50 ns | 45.68 |    0.71 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 21,851.9 ns | 3,876.02 ns | 212.46 ns | 62.86 |    1.10 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  2,597.8 ns |   665.38 ns |  36.47 ns |  7.47 |    0.15 |    3 | 0.2174 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  3,288.0 ns |    32.69 ns |   1.79 ns |  9.46 |    0.15 |    4 | 0.1335 |    2288 B |        8.94 |
