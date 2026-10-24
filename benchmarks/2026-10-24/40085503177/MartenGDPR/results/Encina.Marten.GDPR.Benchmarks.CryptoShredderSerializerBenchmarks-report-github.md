```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    344.0 ns |   322.78 ns |  17.69 ns |  1.00 |    0.06 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    542.3 ns |    56.26 ns |   3.08 ns |  1.58 |    0.07 |    2 | 0.0563 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 17,759.8 ns | 3,838.05 ns | 210.38 ns | 51.72 |    2.31 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 26,822.7 ns | 3,153.02 ns | 172.83 ns | 78.12 |    3.42 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  2,731.7 ns | 1,839.77 ns | 100.84 ns |  7.96 |    0.43 |    3 | 0.2174 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  3,983.1 ns |   718.99 ns |  39.41 ns | 11.60 |    0.51 |    4 | 0.1297 |    2288 B |        8.94 |
