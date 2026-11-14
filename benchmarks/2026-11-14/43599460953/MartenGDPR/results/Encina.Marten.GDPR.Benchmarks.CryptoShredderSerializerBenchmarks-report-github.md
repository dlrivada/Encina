```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.31GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    375.4 ns |  33.59 ns |  1.84 ns |  1.00 |    0.01 |    1 | 0.0029 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    587.9 ns |  19.76 ns |  1.08 ns |  1.57 |    0.01 |    2 | 0.0105 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 18,600.1 ns | 691.35 ns | 37.90 ns | 49.55 |    0.23 |    4 | 0.1221 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 21,607.9 ns | 450.39 ns | 24.69 ns | 57.57 |    0.25 |    4 | 0.1221 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  3,637.0 ns |  37.79 ns |  2.07 ns |  9.69 |    0.04 |    3 | 0.0420 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  3,666.4 ns |  10.49 ns |  0.57 ns |  9.77 |    0.04 |    3 | 0.0267 |    2288 B |        8.94 |
