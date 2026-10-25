```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                            | Mean        | Error     | StdDev      | Median      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|----------:|------------:|------------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    619.2 ns |   1.51 ns |     2.21 ns |    618.7 ns |  1.00 |    0.00 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    996.2 ns |   5.42 ns |     7.78 ns |    997.4 ns |  1.61 |    0.01 |    2 | 0.0553 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 33,562.7 ns | 601.61 ns |   843.37 ns | 34,153.8 ns | 54.20 |    1.35 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 47,191.7 ns | 750.37 ns | 1,076.16 ns | 47,160.8 ns | 76.21 |    1.73 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,383.1 ns |  44.98 ns |    64.51 ns |  5,371.3 ns |  8.69 |    0.11 |    3 | 0.2136 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  7,427.7 ns |  24.86 ns |    36.44 ns |  7,418.3 ns | 12.00 |    0.07 |    4 | 0.1297 |    2288 B |        8.94 |
