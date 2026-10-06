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
| Stream_SmallDataset_10Items             |  12.261 μs |  1.5142 μs | 0.0830 μs |  1.00 |    0.01 |  0.7629 |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  81.924 μs |  9.0555 μs | 0.4964 μs |  6.68 |    0.05 |  5.1270 |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 773.165 μs | 80.4878 μs | 4.4118 μs | 63.06 |    0.48 | 48.8281 |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  68.099 μs |  4.6293 μs | 0.2537 μs |  5.55 |    0.04 |  3.0518 |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  87.479 μs | 10.1584 μs | 0.5568 μs |  7.13 |    0.06 |  5.7373 |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  81.315 μs | 34.0733 μs | 1.8677 μs |  6.63 |    0.14 |  5.1270 |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 127.172 μs | 13.0114 μs | 0.7132 μs | 10.37 |    0.08 |  6.1035 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   4.058 μs |  0.3383 μs | 0.0185 μs |  0.33 |    0.00 |  0.0229 |     400 B |        0.03 |
