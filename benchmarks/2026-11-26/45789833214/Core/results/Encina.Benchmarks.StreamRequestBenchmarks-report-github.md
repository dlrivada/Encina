```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.678 μs |  1.5131 μs | 0.0829 μs |  1.00 |    0.01 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  82.931 μs |  4.8312 μs | 0.2648 μs |  6.54 |    0.04 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 815.817 μs | 56.1167 μs | 3.0759 μs | 64.35 |    0.42 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  68.782 μs |  5.7785 μs | 0.3167 μs |  5.43 |    0.04 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  86.213 μs |  8.3217 μs | 0.4561 μs |  6.80 |    0.05 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  82.745 μs | 16.9643 μs | 0.9299 μs |  6.53 |    0.07 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 126.507 μs | 17.6150 μs | 0.9655 μs |  9.98 |    0.09 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.976 μs |  0.3219 μs | 0.0176 μs |  0.31 |    0.00 |  0.0229 |     400 B |        0.03 |
