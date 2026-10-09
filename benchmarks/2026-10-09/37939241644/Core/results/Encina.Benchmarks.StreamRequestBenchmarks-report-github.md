```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.531 μs |  0.6595 μs | 0.0362 μs |  1.00 |    0.00 |  0.7629 |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  85.072 μs | 14.4711 μs | 0.7932 μs |  6.79 |    0.06 |  5.1270 |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 798.814 μs | 62.1148 μs | 3.4047 μs | 63.75 |    0.28 | 48.8281 |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  68.922 μs |  3.5830 μs | 0.1964 μs |  5.50 |    0.02 |  3.0518 |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  90.656 μs |  1.3905 μs | 0.0762 μs |  7.23 |    0.02 |  5.7373 |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  83.799 μs |  2.9481 μs | 0.1616 μs |  6.69 |    0.02 |  5.1270 |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 130.375 μs |  4.5254 μs | 0.2481 μs | 10.40 |    0.03 |  6.1035 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   4.074 μs |  0.8130 μs | 0.0446 μs |  0.33 |    0.00 |  0.0229 |     400 B |        0.03 |
