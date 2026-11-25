```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.57GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    434.9 ns |    48.54 ns |   2.66 ns |  1.00 |    0.01 |    1 | 0.0029 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    659.6 ns |   127.04 ns |   6.96 ns |  1.52 |    0.02 |    2 | 0.0105 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 23,455.9 ns | 3,785.17 ns | 207.48 ns | 53.93 |    0.50 |    4 | 0.1221 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 26,585.0 ns | 3,122.27 ns | 171.14 ns | 61.13 |    0.47 |    4 | 0.1221 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  4,094.7 ns |   769.39 ns |  42.17 ns |  9.42 |    0.10 |    3 | 0.0381 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  4,385.0 ns |   329.37 ns |  18.05 ns | 10.08 |    0.06 |    3 | 0.0229 |    2288 B |        8.94 |
