```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.176 μs |  0.7054 μs | 0.0387 μs |  1.00 |    0.00 |  0.7629 |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  79.843 μs | 22.5472 μs | 1.2359 μs |  6.56 |    0.09 |  5.1270 |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 770.501 μs | 80.4463 μs | 4.4095 μs | 63.28 |    0.36 | 48.8281 |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  67.811 μs |  3.4789 μs | 0.1907 μs |  5.57 |    0.02 |  3.0518 |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  84.723 μs |  1.1517 μs | 0.0631 μs |  6.96 |    0.02 |  5.7373 |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  80.942 μs |  7.4223 μs | 0.4068 μs |  6.65 |    0.03 |  5.1270 |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 126.504 μs |  5.6329 μs | 0.3088 μs | 10.39 |    0.04 |  6.1035 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   4.002 μs |  1.0749 μs | 0.0589 μs |  0.33 |    0.00 |  0.0229 |     400 B |        0.03 |
