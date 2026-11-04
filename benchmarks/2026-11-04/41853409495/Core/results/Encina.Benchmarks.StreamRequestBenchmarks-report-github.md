```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|-----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.495 μs |   1.5437 μs |  0.0846 μs |  1.00 |    0.01 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  83.159 μs |   8.0065 μs |  0.4389 μs |  6.66 |    0.05 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 776.586 μs | 182.6539 μs | 10.0119 μs | 62.15 |    0.78 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  67.679 μs |   5.2185 μs |  0.2860 μs |  5.42 |    0.04 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  89.454 μs |   9.6656 μs |  0.5298 μs |  7.16 |    0.06 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  82.720 μs |   4.9689 μs |  0.2724 μs |  6.62 |    0.04 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 128.996 μs |  15.7074 μs |  0.8610 μs | 10.32 |    0.09 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.001 μs |   0.6559 μs |  0.0360 μs |  0.32 |    0.00 |  0.0229 |     400 B |        0.03 |
