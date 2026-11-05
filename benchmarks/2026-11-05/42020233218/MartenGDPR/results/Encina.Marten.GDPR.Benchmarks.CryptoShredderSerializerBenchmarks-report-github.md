```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    638.3 ns |   206.30 ns |  11.31 ns |  1.00 |    0.02 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |  1,027.7 ns |    21.23 ns |   1.16 ns |  1.61 |    0.02 |    2 | 0.0553 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 29,698.6 ns | 1,584.49 ns |  86.85 ns | 46.54 |    0.72 |    4 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 38,174.0 ns | 2,306.36 ns | 126.42 ns | 59.82 |    0.93 |    5 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,391.6 ns |   315.78 ns |  17.31 ns |  8.45 |    0.13 |    3 | 0.2136 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  6,413.1 ns |   562.56 ns |  30.84 ns | 10.05 |    0.16 |    3 | 0.1297 |    2288 B |        8.94 |
