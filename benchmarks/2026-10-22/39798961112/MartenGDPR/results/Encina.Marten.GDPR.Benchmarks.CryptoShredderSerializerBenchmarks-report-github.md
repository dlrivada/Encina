```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                            | Mean        | Error       | StdDev      | Median      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|------------:|------------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    658.1 ns |    24.15 ns |    35.40 ns |    632.0 ns |  1.00 |    0.07 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    971.0 ns |     7.32 ns |    10.73 ns |    967.6 ns |  1.48 |    0.08 |    2 | 0.0553 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 33,348.5 ns |   778.43 ns | 1,091.25 ns | 34,299.1 ns | 50.81 |    3.14 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 47,478.2 ns | 1,061.87 ns | 1,556.47 ns | 48,010.4 ns | 72.34 |    4.47 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,554.0 ns |    74.63 ns |   111.70 ns |  5,538.0 ns |  8.46 |    0.48 |    3 | 0.2136 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  7,561.6 ns |    42.35 ns |    62.08 ns |  7,593.8 ns | 11.52 |    0.61 |    4 | 0.1221 |    2288 B |        8.94 |
