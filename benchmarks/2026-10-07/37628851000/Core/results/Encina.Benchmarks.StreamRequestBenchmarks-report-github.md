```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   6.326 μs |  2.7303 μs | 0.1497 μs |  1.00 |    0.03 |  0.7629 |      - |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  41.119 μs |  1.0387 μs | 0.0569 μs |  6.50 |    0.13 |  5.1880 |      - |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 405.215 μs | 84.8863 μs | 4.6529 μs | 64.08 |    1.45 | 49.3164 | 0.4883 |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  36.899 μs |  4.6143 μs | 0.2529 μs |  5.84 |    0.12 |  3.0518 |      - |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  43.743 μs |  6.7672 μs | 0.3709 μs |  6.92 |    0.15 |  5.7373 | 0.0610 |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  42.603 μs | 12.4112 μs | 0.6803 μs |  6.74 |    0.17 |  5.1880 |      - |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  62.779 μs |  5.4602 μs | 0.2993 μs |  9.93 |    0.21 |  6.1035 | 0.1221 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   2.303 μs |  0.3449 μs | 0.0189 μs |  0.36 |    0.01 |  0.0229 |      - |     400 B |        0.03 |
