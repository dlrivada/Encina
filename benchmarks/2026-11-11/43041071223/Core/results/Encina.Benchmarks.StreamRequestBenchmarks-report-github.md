```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|-----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.409 μs |   2.1758 μs |  0.1193 μs |  1.00 |    0.01 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  83.246 μs |   7.3930 μs |  0.4052 μs |  6.71 |    0.06 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 775.914 μs | 189.8264 μs | 10.4050 μs | 62.53 |    0.90 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  66.518 μs |   3.6120 μs |  0.1980 μs |  5.36 |    0.05 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  87.498 μs |  16.5203 μs |  0.9055 μs |  7.05 |    0.09 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  81.106 μs |   8.3648 μs |  0.4585 μs |  6.54 |    0.06 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 125.997 μs |  17.2339 μs |  0.9446 μs | 10.15 |    0.11 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.095 μs |   0.5431 μs |  0.0298 μs |  0.33 |    0.00 |  0.0229 |     400 B |        0.03 |
