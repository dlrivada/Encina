```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.245 μs |  1.236 μs | 0.0677 μs |  1.00 |    0.01 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  80.596 μs | 10.162 μs | 0.5570 μs |  6.58 |    0.05 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 789.593 μs | 68.387 μs | 3.7485 μs | 64.48 |    0.41 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  66.569 μs |  7.219 μs | 0.3957 μs |  5.44 |    0.04 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  84.975 μs |  4.567 μs | 0.2503 μs |  6.94 |    0.04 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  80.535 μs |  7.761 μs | 0.4254 μs |  6.58 |    0.04 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 125.455 μs |  9.295 μs | 0.5095 μs | 10.25 |    0.06 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.998 μs |  1.182 μs | 0.0648 μs |  0.33 |    0.00 |  0.0229 |     400 B |        0.03 |
