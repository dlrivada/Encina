```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.77GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|-----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    392.4 ns |   176.1 ns |   9.65 ns |  1.00 |    0.03 |    1 | 0.0029 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    655.8 ns |   117.5 ns |   6.44 ns |  1.67 |    0.04 |    2 | 0.0105 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 22,599.9 ns | 4,227.2 ns | 231.71 ns | 57.61 |    1.34 |    5 | 0.1221 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 28,895.0 ns | 8,255.0 ns | 452.48 ns | 73.66 |    1.87 |    6 | 0.1221 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  3,713.3 ns |   819.8 ns |  44.94 ns |  9.47 |    0.23 |    3 | 0.0381 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  5,008.5 ns | 3,221.3 ns | 176.57 ns | 12.77 |    0.48 |    4 | 0.0229 |    2288 B |        8.94 |
