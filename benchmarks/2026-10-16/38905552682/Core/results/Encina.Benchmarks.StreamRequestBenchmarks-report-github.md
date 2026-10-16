```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   6.560 μs |  1.6910 μs | 0.0927 μs |  1.00 |    0.02 |  0.7629 |      - |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  41.288 μs | 14.6551 μs | 0.8033 μs |  6.29 |    0.13 |  5.1880 |      - |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 382.246 μs | 39.1743 μs | 2.1473 μs | 58.27 |    0.76 | 49.3164 | 0.4883 |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  35.870 μs | 13.4831 μs | 0.7391 μs |  5.47 |    0.12 |  3.0518 |      - |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  47.309 μs | 83.8247 μs | 4.5947 μs |  7.21 |    0.61 |  5.7373 | 0.0610 |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  43.913 μs |  7.0388 μs | 0.3858 μs |  6.69 |    0.10 |  5.1880 |      - |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  64.588 μs | 11.6148 μs | 0.6366 μs |  9.85 |    0.15 |  6.1035 | 0.1221 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   2.258 μs |  0.5077 μs | 0.0278 μs |  0.34 |    0.01 |  0.0229 |      - |     400 B |        0.03 |
