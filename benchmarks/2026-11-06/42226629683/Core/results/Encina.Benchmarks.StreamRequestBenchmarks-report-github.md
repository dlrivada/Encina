```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.85GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  11.124 μs |  1.4599 μs | 0.0800 μs |  1.00 |    0.01 | 0.1526 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  80.372 μs | 21.3135 μs | 1.1683 μs |  7.23 |    0.10 | 0.9766 |   87024 B |        6.76 |
| Stream_LargeDataset_1000Items           | 758.473 μs |  4.8640 μs | 0.2666 μs | 68.18 |    0.42 | 9.7656 |  828626 B |       64.41 |
| Stream_WithPipelineBehaviors            |  57.841 μs |  3.5247 μs | 0.1932 μs |  5.20 |    0.04 | 0.6104 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  83.061 μs |  9.1902 μs | 0.5037 μs |  7.47 |    0.06 | 1.0986 |   96840 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  78.810 μs |  1.2579 μs | 0.0689 μs |  7.08 |    0.04 | 0.9766 |   87024 B |        6.76 |
| Stream_WithCancellation_EarlyExit       | 117.152 μs |  3.7178 μs | 0.2038 μs | 10.53 |    0.07 | 1.2207 |  102800 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.630 μs |  0.2135 μs | 0.0117 μs |  0.42 |    0.00 |      - |     400 B |        0.03 |
