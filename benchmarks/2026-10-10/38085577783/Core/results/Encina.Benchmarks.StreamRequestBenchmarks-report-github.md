```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.014 μs |   0.5779 μs | 0.0317 μs |  1.00 |    0.00 |  0.7629 |      - |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  61.015 μs |   2.9604 μs | 0.1623 μs |  6.77 |    0.03 |  5.1880 |      - |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 587.778 μs | 120.4567 μs | 6.6026 μs | 65.21 |    0.66 | 48.8281 |      - |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  49.655 μs |   0.8833 μs | 0.0484 μs |  5.51 |    0.02 |  3.0518 |      - |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  65.823 μs |   4.3147 μs | 0.2365 μs |  7.30 |    0.03 |  5.7373 |      - |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  61.700 μs |   4.9078 μs | 0.2690 μs |  6.85 |    0.03 |  5.1880 |      - |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  95.935 μs |  11.2798 μs | 0.6183 μs | 10.64 |    0.07 |  6.1035 | 0.1221 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   3.548 μs |   1.1766 μs | 0.0645 μs |  0.39 |    0.01 |  0.0229 |      - |     400 B |        0.03 |
