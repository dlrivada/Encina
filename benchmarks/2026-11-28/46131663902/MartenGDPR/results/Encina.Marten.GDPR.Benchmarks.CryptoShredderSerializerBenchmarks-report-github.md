```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    375.4 ns |   8.57 ns |  0.47 ns |  1.00 |    0.00 |    1 | 0.0029 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    628.8 ns |  62.70 ns |  3.44 ns |  1.67 |    0.01 |    2 | 0.0105 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 18,055.2 ns | 775.97 ns | 42.53 ns | 48.09 |    0.11 |    4 | 0.1221 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 21,639.9 ns | 304.43 ns | 16.69 ns | 57.64 |    0.07 |    4 | 0.1221 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  3,626.1 ns |  26.83 ns |  1.47 ns |  9.66 |    0.01 |    3 | 0.0420 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  3,694.1 ns | 398.32 ns | 21.83 ns |  9.84 |    0.05 |    3 | 0.0267 |    2288 B |        8.94 |
