```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    685.8 ns |    52.62 ns |  2.88 ns |  1.00 |    0.01 |    1 | 0.0095 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |  1,006.6 ns |    25.44 ns |  1.39 ns |  1.47 |    0.01 |    2 | 0.0362 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 35,166.3 ns |   788.03 ns | 43.19 ns | 51.28 |    0.19 |    5 | 0.3662 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 47,051.9 ns | 1,566.63 ns | 85.87 ns | 68.61 |    0.27 |    6 | 0.4272 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,631.6 ns |   122.12 ns |  6.69 ns |  8.21 |    0.03 |    3 | 0.1450 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  7,820.3 ns |   963.64 ns | 52.82 ns | 11.40 |    0.08 |    4 | 0.0763 |    2288 B |        8.94 |
