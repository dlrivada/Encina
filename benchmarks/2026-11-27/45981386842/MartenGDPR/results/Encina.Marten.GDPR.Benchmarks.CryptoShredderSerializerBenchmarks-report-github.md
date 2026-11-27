```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    519.9 ns |    28.86 ns |   1.58 ns |  1.00 |    0.00 |    1 | 0.0029 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    784.9 ns |    10.18 ns |   0.56 ns |  1.51 |    0.00 |    2 | 0.0105 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 27,029.2 ns | 1,108.28 ns |  60.75 ns | 51.99 |    0.17 |    4 | 0.1221 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 32,001.3 ns | 2,524.23 ns | 138.36 ns | 61.55 |    0.28 |    4 | 0.1221 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  4,863.7 ns |   252.09 ns |  13.82 ns |  9.35 |    0.03 |    3 | 0.0381 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  5,287.1 ns |    63.83 ns |   3.50 ns | 10.17 |    0.03 |    3 | 0.0229 |    2288 B |        8.94 |
