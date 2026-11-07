```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   6.833 μs |   1.5251 μs |  0.0836 μs |  1.00 |    0.02 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  42.849 μs |   8.5590 μs |  0.4691 μs |  6.27 |    0.09 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 416.638 μs | 191.6777 μs | 10.5065 μs | 60.98 |    1.48 | 49.3164 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  40.579 μs |  27.8064 μs |  1.5242 μs |  5.94 |    0.20 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  46.687 μs |  39.4684 μs |  2.1634 μs |  6.83 |    0.28 |  5.7373 | 0.0610 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  42.583 μs |   1.9374 μs |  0.1062 μs |  6.23 |    0.07 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  69.592 μs |  84.2149 μs |  4.6161 μs | 10.19 |    0.60 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   2.300 μs |   0.0955 μs |  0.0052 μs |  0.34 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
