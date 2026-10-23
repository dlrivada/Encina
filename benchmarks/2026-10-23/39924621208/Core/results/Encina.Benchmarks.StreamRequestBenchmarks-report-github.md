```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   7.720 μs |   0.7873 μs | 0.0432 μs |  1.00 |    0.01 | 0.1526 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  55.714 μs |  27.3051 μs | 1.4967 μs |  7.22 |    0.17 | 1.0376 |   87024 B |        6.76 |
| Stream_LargeDataset_1000Items           | 530.765 μs | 161.8789 μs | 8.8731 μs | 68.75 |    1.05 | 9.7656 |  828626 B |       64.41 |
| Stream_WithPipelineBehaviors            |  39.780 μs |   7.9442 μs | 0.4354 μs |  5.15 |    0.05 | 0.6104 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  55.382 μs |  15.8164 μs | 0.8670 μs |  7.17 |    0.10 | 1.0986 |   96840 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  56.396 μs |  44.2557 μs | 2.4258 μs |  7.31 |    0.27 | 1.0376 |   87024 B |        6.76 |
| Stream_WithCancellation_EarlyExit       |  83.363 μs | 100.5633 μs | 5.5122 μs | 10.80 |    0.62 | 1.2207 |  102800 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   2.943 μs |   0.0521 μs | 0.0029 μs |  0.38 |    0.00 | 0.0038 |     400 B |        0.03 |
