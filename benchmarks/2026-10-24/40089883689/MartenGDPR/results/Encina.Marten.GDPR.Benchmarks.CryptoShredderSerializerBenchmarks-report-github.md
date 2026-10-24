```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    689.0 ns |  29.41 ns |  1.61 ns |  1.00 |    0.00 |    1 | 0.0095 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |  1,031.5 ns |  40.11 ns |  2.20 ns |  1.50 |    0.00 |    2 | 0.0362 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 35,160.3 ns | 394.61 ns | 21.63 ns | 51.03 |    0.11 |    5 | 0.3662 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 47,517.1 ns | 703.04 ns | 38.54 ns | 68.97 |    0.15 |    6 | 0.4272 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,755.6 ns | 155.13 ns |  8.50 ns |  8.35 |    0.02 |    3 | 0.1450 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  7,768.0 ns | 567.15 ns | 31.09 ns | 11.27 |    0.05 |    4 | 0.0763 |    2288 B |        8.94 |
