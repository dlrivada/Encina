```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  11.985 μs |   1.9679 μs | 0.1079 μs |  1.00 |    0.01 |  0.7629 |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  80.311 μs |   2.8038 μs | 0.1537 μs |  6.70 |    0.05 |  5.1270 |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 753.199 μs | 120.9880 μs | 6.6318 μs | 62.85 |    0.69 | 48.8281 |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  66.338 μs |   9.5674 μs | 0.5244 μs |  5.54 |    0.06 |  3.0518 |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  88.529 μs |  13.8959 μs | 0.7617 μs |  7.39 |    0.08 |  5.7373 |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  84.947 μs |  11.2487 μs | 0.6166 μs |  7.09 |    0.07 |  5.1270 |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 127.348 μs |   7.4457 μs | 0.4081 μs | 10.63 |    0.09 |  6.1035 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   4.303 μs |   0.9123 μs | 0.0500 μs |  0.36 |    0.00 |  0.0229 |     400 B |        0.03 |
