```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    378.2 ns |    23.24 ns |  1.27 ns |  1.00 |    0.00 |    1 | 0.0029 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    606.3 ns |    25.38 ns |  1.39 ns |  1.60 |    0.01 |    2 | 0.0105 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 18,165.2 ns | 1,409.50 ns | 77.26 ns | 48.04 |    0.23 |    4 | 0.1221 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 21,441.2 ns |   953.24 ns | 52.25 ns | 56.70 |    0.20 |    4 | 0.1221 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  3,654.2 ns |    40.09 ns |  2.20 ns |  9.66 |    0.03 |    3 | 0.0420 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  3,707.8 ns |   429.87 ns | 23.56 ns |  9.80 |    0.06 |    3 | 0.0267 |    2288 B |        8.94 |
