```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.93GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.414 μs |  0.3776 μs | 0.0207 μs |  1.00 |    0.00 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  80.366 μs |  8.5766 μs | 0.4701 μs |  6.47 |    0.03 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 777.766 μs | 72.5667 μs | 3.9776 μs | 62.65 |    0.29 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  68.070 μs | 12.6435 μs | 0.6930 μs |  5.48 |    0.05 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  85.050 μs |  2.2612 μs | 0.1239 μs |  6.85 |    0.01 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  83.163 μs | 15.9437 μs | 0.8739 μs |  6.70 |    0.06 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 125.233 μs | 32.5139 μs | 1.7822 μs | 10.09 |    0.13 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.035 μs |  0.6825 μs | 0.0374 μs |  0.33 |    0.00 |  0.0229 |     400 B |        0.03 |
