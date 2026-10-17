```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.252 μs |  0.2025 μs | 0.0111 μs |  1.00 |    0.00 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  83.221 μs |  6.9689 μs | 0.3820 μs |  6.79 |    0.03 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 790.467 μs | 75.7972 μs | 4.1547 μs | 64.52 |    0.30 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  67.451 μs |  0.1619 μs | 0.0089 μs |  5.51 |    0.00 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  86.331 μs |  8.2025 μs | 0.4496 μs |  7.05 |    0.03 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  80.934 μs |  6.5180 μs | 0.3573 μs |  6.61 |    0.03 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 128.841 μs |  5.2861 μs | 0.2898 μs | 10.52 |    0.02 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.052 μs |  0.7269 μs | 0.0398 μs |  0.33 |    0.00 |  0.0229 |     400 B |        0.03 |
