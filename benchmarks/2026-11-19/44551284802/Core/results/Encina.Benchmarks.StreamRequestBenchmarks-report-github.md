```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   8.342 μs |  4.2980 μs | 0.2356 μs |  1.00 |    0.03 | 0.1526 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  57.911 μs |  5.7863 μs | 0.3172 μs |  6.95 |    0.17 | 1.0376 |   87024 B |        6.76 |
| Stream_LargeDataset_1000Items           | 535.891 μs | 69.0879 μs | 3.7869 μs | 64.27 |    1.60 | 9.7656 |  828626 B |       64.41 |
| Stream_WithPipelineBehaviors            |  41.801 μs |  1.5757 μs | 0.0864 μs |  5.01 |    0.12 | 0.6104 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  61.848 μs |  2.2070 μs | 0.1210 μs |  7.42 |    0.18 | 1.0986 |   96840 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  55.839 μs | 14.9344 μs | 0.8186 μs |  6.70 |    0.18 | 1.0376 |   87024 B |        6.76 |
| Stream_WithCancellation_EarlyExit       |  82.697 μs | 15.5481 μs | 0.8522 μs |  9.92 |    0.26 | 1.2207 |  102800 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.034 μs |  0.1592 μs | 0.0087 μs |  0.36 |    0.01 | 0.0038 |     400 B |        0.03 |
