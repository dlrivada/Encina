```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   6.103 μs |  0.6067 μs | 0.0333 μs |  1.00 |    0.01 |  0.2518 |    4224 B |        1.00 |
| Stream_MediumDataset_100Items           |  35.974 μs |  4.0553 μs | 0.2223 μs |  5.89 |    0.04 |  1.2817 |   22224 B |        5.26 |
| Stream_LargeDataset_1000Items           | 314.441 μs |  5.7974 μs | 0.3178 μs | 51.52 |    0.25 | 11.7188 |  202227 B |       47.88 |
| Stream_WithPipelineBehaviors            |  38.243 μs |  0.8988 μs | 0.0493 μs |  6.27 |    0.03 |  1.0376 |   17952 B |        4.25 |
| Stream_MaterializeToList_100Items       |  37.372 μs |  4.3763 μs | 0.2399 μs |  6.12 |    0.04 |  1.8921 |   32040 B |        7.59 |
| Stream_CountOnly_NoMaterialization      |  39.541 μs |  0.8880 μs | 0.0487 μs |  6.48 |    0.03 |  1.2817 |   22224 B |        5.26 |
| Stream_WithCancellation_EarlyExit       |  67.695 μs | 13.7642 μs | 0.7545 μs | 11.09 |    0.12 |  1.8311 |   32600 B |        7.72 |
| Stream_DirectHandlerInvocation_NoEncina |   4.437 μs |  1.3748 μs | 0.0754 μs |  0.73 |    0.01 |  0.0229 |     400 B |        0.09 |
