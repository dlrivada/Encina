```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|-----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    354.4 ns |   287.8 ns |  15.77 ns |  1.00 |    0.05 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    529.4 ns |   101.8 ns |   5.58 ns |  1.50 |    0.06 |    2 | 0.0563 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 17,688.7 ns | 4,342.8 ns | 238.04 ns | 49.97 |    1.99 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 26,355.6 ns | 6,586.6 ns | 361.03 ns | 74.46 |    2.97 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  2,736.2 ns |   458.4 ns |  25.13 ns |  7.73 |    0.30 |    3 | 0.2174 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  3,938.1 ns | 1,521.6 ns |  83.40 ns | 11.13 |    0.47 |    4 | 0.1297 |    2288 B |        8.94 |
