```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.73GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.687 μs |  2.7541 μs | 0.1510 μs |  1.00 |    0.01 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  84.474 μs | 12.3414 μs | 0.6765 μs |  6.66 |    0.08 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 785.689 μs | 19.9807 μs | 1.0952 μs | 61.94 |    0.64 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  69.106 μs |  2.4976 μs | 0.1369 μs |  5.45 |    0.06 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  86.997 μs |  5.7298 μs | 0.3141 μs |  6.86 |    0.07 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  83.655 μs |  1.3281 μs | 0.0728 μs |  6.59 |    0.07 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 126.931 μs |  3.0876 μs | 0.1692 μs | 10.01 |    0.10 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.983 μs |  0.3207 μs | 0.0176 μs |  0.31 |    0.00 |  0.0229 |     400 B |        0.03 |
