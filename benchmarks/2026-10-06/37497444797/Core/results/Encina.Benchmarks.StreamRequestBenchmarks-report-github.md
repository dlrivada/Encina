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
| Stream_SmallDataset_10Items             |  12.688 μs |  6.1407 μs | 0.3366 μs |  1.00 |    0.03 |  0.7629 |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  83.498 μs | 10.3711 μs | 0.5685 μs |  6.58 |    0.16 |  5.1270 |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 780.486 μs | 84.5958 μs | 4.6370 μs | 61.54 |    1.46 | 48.8281 |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  67.473 μs |  4.4314 μs | 0.2429 μs |  5.32 |    0.12 |  3.0518 |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  84.555 μs |  6.7164 μs | 0.3681 μs |  6.67 |    0.16 |  5.7373 |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  82.875 μs |  2.4499 μs | 0.1343 μs |  6.53 |    0.15 |  5.1270 |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 131.899 μs | 13.8267 μs | 0.7579 μs | 10.40 |    0.25 |  6.1035 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   4.112 μs |  0.4231 μs | 0.0232 μs |  0.32 |    0.01 |  0.0229 |     400 B |        0.03 |
