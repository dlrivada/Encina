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
| InnerSerializer_NonPii            |    377.6 ns |  23.40 ns |  1.28 ns |  1.00 |    0.00 |    1 | 0.0029 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    608.2 ns | 147.65 ns |  8.09 ns |  1.61 |    0.02 |    2 | 0.0105 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 18,612.6 ns | 188.83 ns | 10.35 ns | 49.29 |    0.15 |    4 | 0.1221 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 21,056.0 ns | 655.93 ns | 35.95 ns | 55.76 |    0.18 |    4 | 0.1221 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  3,628.2 ns |  73.55 ns |  4.03 ns |  9.61 |    0.03 |    3 | 0.0420 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  3,713.4 ns | 362.64 ns | 19.88 ns |  9.83 |    0.05 |    3 | 0.0267 |    2288 B |        8.94 |
