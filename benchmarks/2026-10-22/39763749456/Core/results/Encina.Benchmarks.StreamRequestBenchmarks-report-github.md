```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.154 μs |   1.8551 μs | 0.1017 μs |  1.00 |    0.01 | 0.1526 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  64.306 μs |   8.6771 μs | 0.4756 μs |  7.03 |    0.08 | 0.9766 |   87024 B |        6.76 |
| Stream_LargeDataset_1000Items           | 602.091 μs | 144.1718 μs | 7.9025 μs | 65.78 |    0.98 | 9.7656 |  828626 B |       64.41 |
| Stream_WithPipelineBehaviors            |  44.648 μs |   4.6106 μs | 0.2527 μs |  4.88 |    0.05 | 0.6104 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  65.875 μs |  31.7301 μs | 1.7392 μs |  7.20 |    0.18 | 1.0986 |   96840 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  64.013 μs |  12.3134 μs | 0.6749 μs |  6.99 |    0.09 | 0.9766 |   87024 B |        6.76 |
| Stream_WithCancellation_EarlyExit       |  92.269 μs |   7.3522 μs | 0.4030 μs | 10.08 |    0.10 | 1.2207 |  102800 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.368 μs |   0.2113 μs | 0.0116 μs |  0.37 |    0.00 | 0.0038 |     400 B |        0.03 |
