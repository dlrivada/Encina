```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   5.986 μs | 0.0545 μs | 0.0030 μs |  1.00 |    0.00 |  0.2518 |    4224 B |        1.00 |
| Stream_MediumDataset_100Items           |  35.089 μs | 0.6310 μs | 0.0346 μs |  5.86 |    0.01 |  1.2817 |   22224 B |        5.26 |
| Stream_LargeDataset_1000Items           | 328.941 μs | 6.1612 μs | 0.3377 μs | 54.95 |    0.05 | 11.7188 |  202227 B |       47.88 |
| Stream_WithPipelineBehaviors            |  38.827 μs | 5.4537 μs | 0.2989 μs |  6.49 |    0.04 |  1.0376 |   17952 B |        4.25 |
| Stream_MaterializeToList_100Items       |  38.918 μs | 1.5065 μs | 0.0826 μs |  6.50 |    0.01 |  1.8921 |   32040 B |        7.59 |
| Stream_CountOnly_NoMaterialization      |  34.480 μs | 1.8864 μs | 0.1034 μs |  5.76 |    0.02 |  1.2817 |   22224 B |        5.26 |
| Stream_WithCancellation_EarlyExit       |  68.812 μs | 1.4237 μs | 0.0780 μs | 11.50 |    0.01 |  1.8311 |   32600 B |        7.72 |
| Stream_DirectHandlerInvocation_NoEncina |   4.054 μs | 0.7242 μs | 0.0397 μs |  0.68 |    0.01 |  0.0229 |     400 B |        0.09 |
