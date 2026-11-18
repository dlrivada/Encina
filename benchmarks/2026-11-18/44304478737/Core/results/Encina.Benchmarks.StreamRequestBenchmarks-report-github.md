```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   8.338 μs | 0.0676 μs | 0.0037 μs |  1.00 |    0.00 | 0.1526 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  59.596 μs | 2.6455 μs | 0.1450 μs |  7.15 |    0.02 | 1.0376 |   87024 B |        6.76 |
| Stream_LargeDataset_1000Items           | 567.454 μs | 8.2247 μs | 0.4508 μs | 68.06 |    0.05 | 9.7656 |  828626 B |       64.41 |
| Stream_WithPipelineBehaviors            |  43.102 μs | 2.1981 μs | 0.1205 μs |  5.17 |    0.01 | 0.6104 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  62.439 μs | 2.2230 μs | 0.1219 μs |  7.49 |    0.01 | 1.0986 |   96840 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  59.375 μs | 2.8490 μs | 0.1562 μs |  7.12 |    0.02 | 1.0376 |   87024 B |        6.76 |
| Stream_WithCancellation_EarlyExit       |  86.248 μs | 3.2023 μs | 0.1755 μs | 10.34 |    0.02 | 1.2207 |  102800 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.035 μs | 0.0509 μs | 0.0028 μs |  0.36 |    0.00 | 0.0038 |     400 B |        0.03 |
