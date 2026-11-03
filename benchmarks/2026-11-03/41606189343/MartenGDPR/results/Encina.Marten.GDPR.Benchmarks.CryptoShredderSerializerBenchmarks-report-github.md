```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    488.9 ns |    40.18 ns |   2.20 ns |  1.00 |    0.01 |    1 | 0.0029 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    763.3 ns |    92.31 ns |   5.06 ns |  1.56 |    0.01 |    2 | 0.0105 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 25,683.5 ns | 7,236.50 ns | 396.66 ns | 52.53 |    0.73 |    4 | 0.1221 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 30,149.7 ns | 7,538.65 ns | 413.22 ns | 61.66 |    0.77 |    4 | 0.1221 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  4,599.1 ns |   313.92 ns |  17.21 ns |  9.41 |    0.05 |    3 | 0.0381 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  4,999.9 ns | 2,456.93 ns | 134.67 ns | 10.23 |    0.24 |    3 | 0.0229 |    2288 B |        8.94 |
