```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.492 μs |  0.8476 μs | 0.0465 μs |  1.00 |    0.00 |  0.5035 |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  87.970 μs |  8.6052 μs | 0.4717 μs |  7.04 |    0.04 |  3.4180 |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 851.247 μs | 78.3499 μs | 4.2946 μs | 68.14 |    0.37 | 32.2266 |  828624 B |       64.45 |
| Stream_WithPipelineBehaviors            |  71.707 μs |  5.0319 μs | 0.2758 μs |  5.74 |    0.03 |  1.9531 |   51544 B |        4.01 |
| Stream_MaterializeToList_100Items       |  91.029 μs | 12.0216 μs | 0.6589 μs |  7.29 |    0.05 |  3.7842 |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  86.511 μs |  6.1339 μs | 0.3362 μs |  6.93 |    0.03 |  3.4180 |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 134.425 μs | 68.9952 μs | 3.7819 μs | 10.76 |    0.26 |  3.9063 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   4.294 μs |  0.3964 μs | 0.0217 μs |  0.34 |    0.00 |  0.0153 |     400 B |        0.03 |
