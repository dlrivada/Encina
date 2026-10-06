```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii            |    441.5 ns |    35.05 ns |   1.92 ns |  1.00 |    0.01 |    1 | 0.0029 |     256 B |        1.00 |
| CryptoSerializer_NonPii           |    712.9 ns |   328.82 ns |  18.02 ns |  1.61 |    0.04 |    2 | 0.0105 |     944 B |        3.69 |
| CryptoDeserializer_NestedPiiEvent | 27,021.9 ns | 2,988.73 ns | 163.82 ns | 61.21 |    0.40 |    5 | 0.1221 |   10304 B |       40.25 |
| CryptoSerializer_NestedPiiEvent   | 36,901.3 ns | 7,187.94 ns | 394.00 ns | 83.58 |    0.83 |    6 | 0.1221 |   11304 B |       44.16 |
| CryptoSerializer_NonPiiNested     |  4,385.6 ns |   642.02 ns |  35.19 ns |  9.93 |    0.08 |    3 | 0.0381 |    3656 B |       14.28 |
| CryptoSerializer_PiiEvent         |  5,774.3 ns | 2,325.12 ns | 127.45 ns | 13.08 |    0.25 |    4 | 0.0229 |    2288 B |        8.94 |
