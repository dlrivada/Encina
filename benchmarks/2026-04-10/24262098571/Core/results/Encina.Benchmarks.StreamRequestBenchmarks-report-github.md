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
| Stream_SmallDataset_10Items             |   6.013 μs | 0.1158 μs | 0.0063 μs |  1.00 |    0.00 |  0.2518 |    4224 B |        1.00 |
| Stream_MediumDataset_100Items           |  36.200 μs | 0.3497 μs | 0.0192 μs |  6.02 |    0.01 |  1.2817 |   22224 B |        5.26 |
| Stream_LargeDataset_1000Items           | 326.722 μs | 3.8707 μs | 0.2122 μs | 54.33 |    0.06 | 11.7188 |  202227 B |       47.88 |
| Stream_WithPipelineBehaviors            |  38.621 μs | 3.7034 μs | 0.2030 μs |  6.42 |    0.03 |  1.0376 |   17952 B |        4.25 |
| Stream_MaterializeToList_100Items       |  37.753 μs | 2.6600 μs | 0.1458 μs |  6.28 |    0.02 |  1.8921 |   32040 B |        7.59 |
| Stream_CountOnly_NoMaterialization      |  35.453 μs | 3.0431 μs | 0.1668 μs |  5.90 |    0.02 |  1.2817 |   22224 B |        5.26 |
| Stream_WithCancellation_EarlyExit       |  71.436 μs | 3.7458 μs | 0.2053 μs | 11.88 |    0.03 |  1.8311 |   32600 B |        7.72 |
| Stream_DirectHandlerInvocation_NoEncina |   4.036 μs | 0.4914 μs | 0.0269 μs |  0.67 |    0.00 |  0.0229 |     400 B |        0.09 |
