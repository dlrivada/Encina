```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.572 μs |  0.0379 μs | 0.0021 μs |  1.00 |    0.00 |  0.5341 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  63.792 μs |  1.9572 μs | 0.1073 μs |  6.66 |    0.01 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 607.068 μs | 14.1834 μs | 0.7774 μs | 63.42 |    0.07 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  55.696 μs |  2.4606 μs | 0.1349 μs |  5.82 |    0.01 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  67.882 μs |  6.0143 μs | 0.3297 μs |  7.09 |    0.03 |  4.0283 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  66.525 μs |  5.0054 μs | 0.2744 μs |  6.95 |    0.02 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       | 109.125 μs |  2.7854 μs | 0.1527 μs | 11.40 |    0.01 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.084 μs |  0.3617 μs | 0.0198 μs |  0.43 |    0.00 |  0.0229 |      - |     400 B |        0.04 |
