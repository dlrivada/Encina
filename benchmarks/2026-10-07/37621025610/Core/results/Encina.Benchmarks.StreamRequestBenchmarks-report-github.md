```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.642 μs |  5.273 μs | 0.2890 μs |  1.00 |    0.03 |  0.7629 |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  87.272 μs |  9.477 μs | 0.5194 μs |  6.91 |    0.14 |  5.1270 |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 784.089 μs | 88.832 μs | 4.8692 μs | 62.05 |    1.26 | 48.8281 |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  69.325 μs |  7.636 μs | 0.4185 μs |  5.49 |    0.11 |  3.0518 |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  89.991 μs |  1.625 μs | 0.0891 μs |  7.12 |    0.14 |  5.7373 |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  85.147 μs |  2.750 μs | 0.1507 μs |  6.74 |    0.13 |  5.1270 |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 125.458 μs | 11.638 μs | 0.6379 μs |  9.93 |    0.20 |  6.1035 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   4.132 μs |  1.126 μs | 0.0617 μs |  0.33 |    0.01 |  0.0229 |     400 B |        0.03 |
