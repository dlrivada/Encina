```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.08GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|-------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    495.4 ns |     14.63 ns |   0.80 ns |  1.00 |    0.00 |    1 | 0.0029 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    777.9 ns |     36.45 ns |   2.00 ns |  1.57 |    0.00 |    2 | 0.0105 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 29,227.1 ns |    815.63 ns |  44.71 ns | 59.00 |    0.11 |    5 | 0.1221 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 38,699.7 ns | 10,933.05 ns | 599.28 ns | 78.13 |    1.05 |    6 | 0.1221 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  4,721.5 ns |    455.30 ns |  24.96 ns |  9.53 |    0.05 |    3 | 0.0381 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  6,093.2 ns |    199.64 ns |  10.94 ns | 12.30 |    0.03 |    4 | 0.0229 |    2288 B |        8.94 |
