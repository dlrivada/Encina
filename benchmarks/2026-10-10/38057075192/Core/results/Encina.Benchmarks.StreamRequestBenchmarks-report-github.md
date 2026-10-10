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
| Stream_SmallDataset_10Items             |  12.754 μs |  0.8462 μs | 0.0464 μs |  1.00 |    0.00 |  0.7629 |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  85.076 μs | 18.2794 μs | 1.0020 μs |  6.67 |    0.07 |  5.1270 |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 808.265 μs | 26.2605 μs | 1.4394 μs | 63.37 |    0.22 | 48.8281 |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  68.828 μs |  7.3120 μs | 0.4008 μs |  5.40 |    0.03 |  3.0518 |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  90.693 μs |  4.6310 μs | 0.2538 μs |  7.11 |    0.03 |  5.7373 |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  85.574 μs |  3.2126 μs | 0.1761 μs |  6.71 |    0.02 |  5.1270 |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 129.617 μs |  8.1378 μs | 0.4461 μs | 10.16 |    0.04 |  6.1035 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   4.129 μs |  0.8417 μs | 0.0461 μs |  0.32 |    0.00 |  0.0229 |     400 B |        0.03 |
