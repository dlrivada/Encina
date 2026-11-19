```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.501 μs |   2.250 μs | 0.1233 μs |  1.00 |    0.02 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  64.958 μs |  10.491 μs | 0.5750 μs |  6.84 |    0.09 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 591.652 μs | 114.337 μs | 6.2672 μs | 62.28 |    0.91 | 48.8281 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  50.351 μs |   6.378 μs | 0.3496 μs |  5.30 |    0.07 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  65.380 μs |  14.087 μs | 0.7721 μs |  6.88 |    0.10 |  5.7373 |      - |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  63.631 μs |  15.004 μs | 0.8224 μs |  6.70 |    0.11 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  93.420 μs |   4.959 μs | 0.2718 μs |  9.83 |    0.11 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.485 μs |   1.505 μs | 0.0825 μs |  0.37 |    0.01 |  0.0229 |      - |     400 B |        0.03 |
