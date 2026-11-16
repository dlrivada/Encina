```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   8.418 μs |  0.1481 μs | 0.0081 μs |  1.00 |    0.00 | 0.1526 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  60.181 μs |  2.6306 μs | 0.1442 μs |  7.15 |    0.02 | 1.0376 |   87024 B |        6.76 |
| Stream_LargeDataset_1000Items           | 571.187 μs | 33.1996 μs | 1.8198 μs | 67.85 |    0.20 | 9.7656 |  828626 B |       64.41 |
| Stream_WithPipelineBehaviors            |  42.975 μs |  1.7098 μs | 0.0937 μs |  5.11 |    0.01 | 0.6104 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  62.572 μs |  3.0830 μs | 0.1690 μs |  7.43 |    0.02 | 1.0986 |   96840 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  59.762 μs |  0.4790 μs | 0.0263 μs |  7.10 |    0.01 | 1.0376 |   87024 B |        6.76 |
| Stream_WithCancellation_EarlyExit       |  85.751 μs |  3.2645 μs | 0.1789 μs | 10.19 |    0.02 | 1.2207 |  102800 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.087 μs |  0.1223 μs | 0.0067 μs |  0.37 |    0.00 | 0.0038 |     400 B |        0.03 |
