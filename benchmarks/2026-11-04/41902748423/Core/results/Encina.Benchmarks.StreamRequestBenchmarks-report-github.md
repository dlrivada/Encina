```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.914 μs |  4.9677 μs | 0.2723 μs |  1.00 |    0.03 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  84.311 μs |  8.1229 μs | 0.4452 μs |  6.53 |    0.12 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 792.600 μs | 34.2292 μs | 1.8762 μs | 61.40 |    1.14 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  77.948 μs |  2.5527 μs | 0.1399 μs |  6.04 |    0.11 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  91.582 μs |  6.8665 μs | 0.3764 μs |  7.09 |    0.13 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  84.408 μs |  1.8448 μs | 0.1011 μs |  6.54 |    0.12 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 129.284 μs |  5.6882 μs | 0.3118 μs | 10.01 |    0.19 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.953 μs |  0.3760 μs | 0.0206 μs |  0.31 |    0.01 |  0.0229 |     400 B |        0.03 |
