```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.444 μs |  0.3777 μs | 0.0207 μs |  1.00 |    0.00 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  83.362 μs |  4.5322 μs | 0.2484 μs |  6.70 |    0.02 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 780.770 μs | 79.0075 μs | 4.3307 μs | 62.74 |    0.31 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  65.805 μs |  4.6561 μs | 0.2552 μs |  5.29 |    0.02 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  86.286 μs |  1.5626 μs | 0.0857 μs |  6.93 |    0.01 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  84.072 μs |  6.8694 μs | 0.3765 μs |  6.76 |    0.03 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 123.340 μs |  3.1669 μs | 0.1736 μs |  9.91 |    0.02 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.169 μs |  0.6847 μs | 0.0375 μs |  0.33 |    0.00 |  0.0229 |     400 B |        0.03 |
