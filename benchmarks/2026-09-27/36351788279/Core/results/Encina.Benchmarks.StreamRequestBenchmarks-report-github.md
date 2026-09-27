```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   7.626 μs |  2.292 μs | 0.1256 μs |  1.00 |    0.02 | 0.1068 |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  54.138 μs |  4.437 μs | 0.2432 μs |  7.10 |    0.11 | 0.6714 |   58848 B |        6.42 |
| Stream_LargeDataset_1000Items           | 515.370 μs | 76.472 μs | 4.1917 μs | 67.60 |    1.08 | 5.8594 |  555650 B |       60.61 |
| Stream_WithPipelineBehaviors            |  43.986 μs |  7.070 μs | 0.3875 μs |  5.77 |    0.09 | 0.4272 |   36976 B |        4.03 |
| Stream_MaterializeToList_100Items       |  57.455 μs | 10.744 μs | 0.5889 μs |  7.54 |    0.13 | 0.7324 |   68664 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  54.934 μs |  9.520 μs | 0.5218 μs |  7.21 |    0.12 | 0.6714 |   58848 B |        6.42 |
| Stream_WithCancellation_EarlyExit       |  88.150 μs | 64.374 μs | 3.5285 μs | 11.56 |    0.43 | 0.8545 |   74624 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   3.986 μs |  1.483 μs | 0.0813 μs |  0.52 |    0.01 |      - |     400 B |        0.04 |
