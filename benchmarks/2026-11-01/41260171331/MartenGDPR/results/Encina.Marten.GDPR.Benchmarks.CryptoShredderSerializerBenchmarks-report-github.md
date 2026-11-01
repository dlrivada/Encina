```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                            | Mean        | Error    | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|---------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    481.1 ns |  8.43 ns |  11.82 ns |  1.00 |    0.03 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    750.2 ns |  9.93 ns |  13.92 ns |  1.56 |    0.05 |    2 | 0.0563 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 25,172.0 ns | 52.34 ns |  75.07 ns | 52.35 |    1.27 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 35,637.5 ns | 97.13 ns | 139.30 ns | 74.12 |    1.81 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  3,916.2 ns | 45.12 ns |  66.13 ns |  8.14 |    0.24 |    3 | 0.2136 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  5,504.0 ns | 45.33 ns |  63.55 ns | 11.45 |    0.31 |    4 | 0.1297 |    2288 B |        8.94 |
