```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    588.3 ns |    33.77 ns |   1.85 ns |  1.00 |    0.00 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    957.6 ns |    48.28 ns |   2.65 ns |  1.63 |    0.01 |    2 | 0.0553 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 31,930.9 ns | 4,109.24 ns | 225.24 ns | 54.27 |    0.36 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 48,224.4 ns | 2,937.89 ns | 161.04 ns | 81.97 |    0.33 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,418.6 ns |   412.80 ns |  22.63 ns |  9.21 |    0.04 |    3 | 0.2136 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  7,392.0 ns |   323.52 ns |  17.73 ns | 12.56 |    0.04 |    4 | 0.1297 |    2288 B |        8.94 |
