```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  11.134 μs |  0.6772 μs | 0.0371 μs |  1.00 |    0.00 | 0.1526 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  79.021 μs |  2.3069 μs | 0.1264 μs |  7.10 |    0.02 | 0.9766 |   87024 B |        6.76 |
| Stream_LargeDataset_1000Items           | 760.460 μs | 12.6051 μs | 0.6909 μs | 68.30 |    0.20 | 9.7656 |  828626 B |       64.41 |
| Stream_WithPipelineBehaviors            |  58.061 μs |  3.9889 μs | 0.2186 μs |  5.21 |    0.02 | 0.6104 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  82.838 μs |  0.8661 μs | 0.0475 μs |  7.44 |    0.02 | 1.0986 |   96840 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  79.353 μs |  0.7357 μs | 0.0403 μs |  7.13 |    0.02 | 0.9766 |   87024 B |        6.76 |
| Stream_WithCancellation_EarlyExit       | 116.265 μs |  7.0193 μs | 0.3848 μs | 10.44 |    0.04 | 1.2207 |  102800 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.491 μs |  0.2158 μs | 0.0118 μs |  0.40 |    0.00 |      - |     400 B |        0.03 |
