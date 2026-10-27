```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|------------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    362.2 ns |    206.7 ns |    11.33 ns |  1.00 |    0.04 |    1 | 0.0029 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    622.0 ns |    203.8 ns |    11.17 ns |  1.72 |    0.05 |    2 | 0.0105 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 20,608.3 ns | 11,801.7 ns |   646.89 ns | 56.94 |    2.17 |    5 | 0.1221 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 27,034.8 ns | 19,881.0 ns | 1,089.75 ns | 74.69 |    3.28 |    6 | 0.1221 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  3,413.8 ns |    356.3 ns |    19.53 ns |  9.43 |    0.26 |    3 | 0.0420 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  4,895.2 ns |  3,096.5 ns |   169.73 ns | 13.52 |    0.54 |    4 | 0.0229 |    2288 B |        8.94 |
