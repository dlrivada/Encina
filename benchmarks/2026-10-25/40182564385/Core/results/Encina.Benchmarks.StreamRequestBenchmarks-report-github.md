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
| Stream_SmallDataset_10Items             |  12.163 μs |  0.2131 μs | 0.0117 μs |  1.00 |    0.00 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  82.328 μs |  5.6980 μs | 0.3123 μs |  6.77 |    0.02 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 767.257 μs | 94.5578 μs | 5.1830 μs | 63.08 |    0.37 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  67.684 μs |  2.5181 μs | 0.1380 μs |  5.56 |    0.01 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  86.695 μs | 10.2039 μs | 0.5593 μs |  7.13 |    0.04 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  80.827 μs |  3.8836 μs | 0.2129 μs |  6.65 |    0.02 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 130.882 μs | 13.1108 μs | 0.7186 μs | 10.76 |    0.05 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.129 μs |  2.3384 μs | 0.1282 μs |  0.34 |    0.01 |  0.0229 |     400 B |        0.03 |
