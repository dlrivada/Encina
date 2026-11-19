```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    659.3 ns |   114.69 ns |   6.29 ns |  1.00 |    0.01 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    961.1 ns |    43.27 ns |   2.37 ns |  1.46 |    0.01 |    2 | 0.0553 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 31,843.6 ns | 5,045.41 ns | 276.56 ns | 48.30 |    0.54 |    4 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 40,292.8 ns | 3,873.05 ns | 212.30 ns | 61.11 |    0.58 |    5 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,607.6 ns |   483.83 ns |  26.52 ns |  8.51 |    0.08 |    3 | 0.2136 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  6,404.0 ns |   595.17 ns |  32.62 ns |  9.71 |    0.09 |    3 | 0.1297 |    2288 B |        8.94 |
