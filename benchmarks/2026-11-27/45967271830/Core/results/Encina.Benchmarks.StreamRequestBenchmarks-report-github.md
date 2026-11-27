```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.49GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   7.077 μs |  0.1895 μs | 0.0104 μs |  1.00 |    0.00 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  43.805 μs | 17.6441 μs | 0.9671 μs |  6.19 |    0.12 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 416.564 μs | 99.8799 μs | 5.4748 μs | 58.86 |    0.67 | 49.3164 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  40.844 μs | 42.8694 μs | 2.3498 μs |  5.77 |    0.29 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  47.183 μs | 10.5111 μs | 0.5761 μs |  6.67 |    0.07 |  5.7373 | 0.0610 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  45.420 μs |  7.5979 μs | 0.4165 μs |  6.42 |    0.05 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  66.039 μs | 12.6841 μs | 0.6953 μs |  9.33 |    0.09 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   2.273 μs |  1.0492 μs | 0.0575 μs |  0.32 |    0.01 |  0.0229 |      - |     400 B |        0.03 |
