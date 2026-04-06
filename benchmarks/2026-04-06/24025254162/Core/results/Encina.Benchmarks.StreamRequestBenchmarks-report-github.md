```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   5.861 μs |  0.1017 μs | 0.0056 μs |  1.00 |    0.00 |  0.2518 |    4224 B |        1.00 |
| Stream_MediumDataset_100Items           |  34.950 μs |  1.8749 μs | 0.1028 μs |  5.96 |    0.02 |  1.2817 |   22224 B |        5.26 |
| Stream_LargeDataset_1000Items           | 320.236 μs | 59.3773 μs | 3.2547 μs | 54.63 |    0.48 | 11.7188 |  202227 B |       47.88 |
| Stream_WithPipelineBehaviors            |  37.798 μs |  2.2999 μs | 0.1261 μs |  6.45 |    0.02 |  1.0376 |   17952 B |        4.25 |
| Stream_MaterializeToList_100Items       |  37.134 μs |  0.9092 μs | 0.0498 μs |  6.34 |    0.01 |  1.8921 |   32040 B |        7.59 |
| Stream_CountOnly_NoMaterialization      |  34.771 μs |  3.1951 μs | 0.1751 μs |  5.93 |    0.03 |  1.2817 |   22224 B |        5.26 |
| Stream_WithCancellation_EarlyExit       |  69.539 μs |  3.6120 μs | 0.1980 μs | 11.86 |    0.03 |  1.8311 |   32600 B |        7.72 |
| Stream_DirectHandlerInvocation_NoEncina |   4.069 μs |  0.6937 μs | 0.0380 μs |  0.69 |    0.01 |  0.0229 |     400 B |        0.09 |
