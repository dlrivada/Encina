```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.126 μs |  0.6136 μs | 0.0336 μs |  1.00 |    0.00 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  81.182 μs |  3.8358 μs | 0.2103 μs |  6.70 |    0.02 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 787.018 μs | 44.5559 μs | 2.4423 μs | 64.91 |    0.23 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  68.356 μs | 11.4107 μs | 0.6255 μs |  5.64 |    0.05 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  85.643 μs |  6.9596 μs | 0.3815 μs |  7.06 |    0.03 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  82.950 μs |  2.6680 μs | 0.1462 μs |  6.84 |    0.02 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 124.875 μs | 12.5223 μs | 0.6864 μs | 10.30 |    0.05 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.054 μs |  0.9667 μs | 0.0530 μs |  0.33 |    0.00 |  0.0229 |     400 B |        0.03 |
