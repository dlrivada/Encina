```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   5.916 μs | 0.0666 μs | 0.0997 μs |  1.00 |    0.02 |  0.2518 |    4224 B |        1.00 |
| Stream_MediumDataset_100Items           |  35.041 μs | 0.3308 μs | 0.4637 μs |  5.92 |    0.12 |  1.2817 |   22224 B |        5.26 |
| Stream_LargeDataset_1000Items           | 322.860 μs | 2.9219 μs | 4.3734 μs | 54.59 |    1.16 | 11.7188 |  202227 B |       47.88 |
| Stream_WithPipelineBehaviors            |  38.277 μs | 0.1563 μs | 0.2190 μs |  6.47 |    0.11 |  1.0376 |   17952 B |        4.25 |
| Stream_MaterializeToList_100Items       |  38.911 μs | 0.3709 μs | 0.5437 μs |  6.58 |    0.14 |  1.8921 |   32040 B |        7.59 |
| Stream_CountOnly_NoMaterialization      |  35.116 μs | 0.1335 μs | 0.1998 μs |  5.94 |    0.10 |  1.2817 |   22224 B |        5.26 |
| Stream_WithCancellation_EarlyExit       |  70.382 μs | 0.2108 μs | 0.3091 μs | 11.90 |    0.20 |  1.8311 |   32600 B |        7.72 |
| Stream_DirectHandlerInvocation_NoEncina |   4.085 μs | 0.0344 μs | 0.0514 μs |  0.69 |    0.01 |  0.0229 |     400 B |        0.09 |
