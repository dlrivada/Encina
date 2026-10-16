```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|-------------:|------------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    626.4 ns |     55.19 ns |     3.03 ns |  1.00 |    0.01 |    1 | 0.0153 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    979.2 ns |    255.53 ns |    14.01 ns |  1.56 |    0.02 |    2 | 0.0553 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 33,470.9 ns | 22,381.01 ns | 1,226.78 ns | 53.44 |    1.71 |    5 | 0.6104 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 46,103.8 ns | 20,692.05 ns | 1,134.20 ns | 73.61 |    1.60 |    6 | 0.6714 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  5,735.1 ns |  1,992.09 ns |   109.19 ns |  9.16 |    0.16 |    3 | 0.2136 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  7,191.8 ns |    247.33 ns |    13.56 ns | 11.48 |    0.05 |    4 | 0.1297 |    2288 B |        8.94 |
