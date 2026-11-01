```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.58GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.520 μs | 0.0534 μs | 0.0800 μs |  1.00 |    0.01 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  85.033 μs | 0.5323 μs | 0.7634 μs |  6.79 |    0.07 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 787.855 μs | 6.4147 μs | 9.1998 μs | 62.93 |    0.82 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  70.598 μs | 0.5649 μs | 0.8280 μs |  5.64 |    0.07 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  87.643 μs | 0.5195 μs | 0.7615 μs |  7.00 |    0.07 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  83.266 μs | 0.4490 μs | 0.6295 μs |  6.65 |    0.06 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 130.298 μs | 0.3895 μs | 0.5709 μs | 10.41 |    0.08 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.017 μs | 0.0137 μs | 0.0193 μs |  0.32 |    0.00 |  0.0229 |     400 B |        0.03 |
