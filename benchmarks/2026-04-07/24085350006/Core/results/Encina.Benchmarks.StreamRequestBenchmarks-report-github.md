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
| Stream_SmallDataset_10Items             |   5.885 μs | 0.3559 μs | 0.0195 μs |  1.00 |    0.00 |  0.2518 |    4224 B |        1.00 |
| Stream_MediumDataset_100Items           |  34.829 μs | 3.0477 μs | 0.1671 μs |  5.92 |    0.03 |  1.2817 |   22224 B |        5.26 |
| Stream_LargeDataset_1000Items           | 330.400 μs | 9.9923 μs | 0.5477 μs | 56.14 |    0.18 | 11.7188 |  202227 B |       47.88 |
| Stream_WithPipelineBehaviors            |  38.880 μs | 2.0386 μs | 0.1117 μs |  6.61 |    0.03 |  1.0376 |   17952 B |        4.25 |
| Stream_MaterializeToList_100Items       |  38.119 μs | 2.4248 μs | 0.1329 μs |  6.48 |    0.03 |  1.8921 |   32040 B |        7.59 |
| Stream_CountOnly_NoMaterialization      |  35.360 μs | 2.2411 μs | 0.1228 μs |  6.01 |    0.02 |  1.2817 |   22224 B |        5.26 |
| Stream_WithCancellation_EarlyExit       |  70.891 μs | 1.1718 μs | 0.0642 μs | 12.05 |    0.04 |  1.8311 |   32600 B |        7.72 |
| Stream_DirectHandlerInvocation_NoEncina |   4.067 μs | 0.5905 μs | 0.0324 μs |  0.69 |    0.01 |  0.0229 |     400 B |        0.09 |
