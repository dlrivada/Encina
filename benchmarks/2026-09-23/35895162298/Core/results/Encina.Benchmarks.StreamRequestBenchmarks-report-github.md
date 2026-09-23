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
| Stream_SmallDataset_10Items             |   6.351 μs |  1.5511 μs | 0.0850 μs |  1.00 |    0.02 |  0.2518 |    4224 B |        1.00 |
| Stream_MediumDataset_100Items           |  37.760 μs |  3.9213 μs | 0.2149 μs |  5.95 |    0.07 |  1.2817 |   22224 B |        5.26 |
| Stream_LargeDataset_1000Items           | 316.729 μs | 24.8136 μs | 1.3601 μs | 49.88 |    0.60 | 11.7188 |  202227 B |       47.88 |
| Stream_WithPipelineBehaviors            |  39.739 μs |  1.2453 μs | 0.0683 μs |  6.26 |    0.07 |  1.0376 |   17952 B |        4.25 |
| Stream_MaterializeToList_100Items       |  38.729 μs |  1.5975 μs | 0.0876 μs |  6.10 |    0.07 |  1.8921 |   32040 B |        7.59 |
| Stream_CountOnly_NoMaterialization      |  35.343 μs |  2.6640 μs | 0.1460 μs |  5.57 |    0.07 |  1.2817 |   22224 B |        5.26 |
| Stream_WithCancellation_EarlyExit       |  70.130 μs |  1.9967 μs | 0.1094 μs | 11.04 |    0.13 |  1.8311 |   32600 B |        7.72 |
| Stream_DirectHandlerInvocation_NoEncina |   4.308 μs |  0.9409 μs | 0.0516 μs |  0.68 |    0.01 |  0.0229 |     400 B |        0.09 |
