```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   6.656 μs |   2.3474 μs | 0.1287 μs |  1.00 |    0.02 |  0.7629 |      - |   12848 B |        1.00 |
| Stream_MediumDataset_100Items           |  41.936 μs |   9.9249 μs | 0.5440 μs |  6.30 |    0.13 |  5.1880 |      - |   87009 B |        6.77 |
| Stream_LargeDataset_1000Items           | 414.912 μs | 102.1815 μs | 5.6009 μs | 62.35 |    1.26 | 49.3164 | 0.4883 |  828620 B |       64.49 |
| Stream_WithPipelineBehaviors            |  37.085 μs |  19.4342 μs | 1.0653 μs |  5.57 |    0.17 |  3.0518 |      - |   51537 B |        4.01 |
| Stream_MaterializeToList_100Items       |  43.304 μs |   1.2900 μs | 0.0707 μs |  6.51 |    0.11 |  5.7373 | 0.0610 |   96825 B |        7.54 |
| Stream_CountOnly_NoMaterialization      |  41.802 μs |  19.8481 μs | 1.0879 μs |  6.28 |    0.18 |  5.1880 |      - |   87009 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  64.015 μs |   8.0364 μs | 0.4405 μs |  9.62 |    0.17 |  6.1035 | 0.1221 |  102785 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   2.283 μs |   0.7331 μs | 0.0402 μs |  0.34 |    0.01 |  0.0229 |      - |     400 B |        0.03 |
