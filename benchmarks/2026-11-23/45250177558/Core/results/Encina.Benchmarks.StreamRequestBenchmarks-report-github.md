```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.42GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   6.525 μs |   0.3305 μs | 0.0181 μs |  1.00 |    0.00 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  45.503 μs |  42.1080 μs | 2.3081 μs |  6.97 |    0.31 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 421.701 μs | 159.5924 μs | 8.7478 μs | 64.63 |    1.17 | 49.3164 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  39.015 μs |   8.2863 μs | 0.4542 μs |  5.98 |    0.06 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  49.130 μs |  98.1731 μs | 5.3812 μs |  7.53 |    0.71 |  5.7373 | 0.0610 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  43.885 μs |   3.8780 μs | 0.2126 μs |  6.73 |    0.03 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  62.955 μs |  27.6306 μs | 1.5145 μs |  9.65 |    0.20 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   2.292 μs |   1.9070 μs | 0.1045 μs |  0.35 |    0.01 |  0.0229 |      - |     400 B |        0.03 |
