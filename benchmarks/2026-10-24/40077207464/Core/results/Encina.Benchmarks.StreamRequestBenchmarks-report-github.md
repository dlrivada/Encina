```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   6.701 μs |  4.849 μs | 0.2658 μs |  1.00 |    0.05 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  43.320 μs |  5.420 μs | 0.2971 μs |  6.47 |    0.22 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 402.530 μs | 51.276 μs | 2.8106 μs | 60.13 |    2.05 | 49.3164 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  37.280 μs | 14.121 μs | 0.7740 μs |  5.57 |    0.21 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  41.368 μs |  7.835 μs | 0.4294 μs |  6.18 |    0.22 |  5.7373 | 0.0610 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  42.038 μs | 44.706 μs | 2.4505 μs |  6.28 |    0.38 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  61.580 μs | 29.197 μs | 1.6004 μs |  9.20 |    0.37 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   2.302 μs |  4.571 μs | 0.2505 μs |  0.34 |    0.03 |  0.0229 |      - |     400 B |        0.03 |
