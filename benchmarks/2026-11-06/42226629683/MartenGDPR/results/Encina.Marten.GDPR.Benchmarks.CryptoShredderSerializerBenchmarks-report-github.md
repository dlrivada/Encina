```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    429.1 ns |   6.72 ns |  0.37 ns |  1.00 |    0.00 |    1 | 0.0029 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    650.2 ns | 144.35 ns |  7.91 ns |  1.52 |    0.02 |    2 | 0.0105 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 23,927.3 ns | 693.34 ns | 38.00 ns | 55.77 |    0.09 |    4 | 0.1221 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 26,350.5 ns | 874.24 ns | 47.92 ns | 61.42 |    0.11 |    4 | 0.1221 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  4,034.9 ns | 116.71 ns |  6.40 ns |  9.40 |    0.01 |    3 | 0.0381 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  4,334.3 ns | 387.16 ns | 21.22 ns | 10.10 |    0.04 |    3 | 0.0229 |    2288 B |        8.94 |
