```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    339.7 ns |   247.66 ns | 13.57 ns |  1.00 |    0.05 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    519.4 ns |    20.27 ns |  1.11 ns |  1.53 |    0.05 |    2 | 0.0563 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 17,192.3 ns |   861.19 ns | 47.20 ns | 50.67 |    1.72 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 25,674.3 ns | 1,659.88 ns | 90.98 ns | 75.66 |    2.57 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  2,722.1 ns | 1,705.74 ns | 93.50 ns |  8.02 |    0.36 |    3 | 0.2174 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  3,910.3 ns |   904.23 ns | 49.56 ns | 11.52 |    0.41 |    4 | 0.1335 |    2288 B |        8.94 |
