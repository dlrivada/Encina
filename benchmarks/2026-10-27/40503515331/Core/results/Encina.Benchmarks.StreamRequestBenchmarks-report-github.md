```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.55GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   8.466 μs |   1.843 μs |  0.1010 μs |  1.00 |    0.01 | 0.1526 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  62.321 μs |   3.383 μs |  0.1855 μs |  7.36 |    0.08 | 0.9766 |   87024 B |        6.76 |
| Stream_LargeDataset_1000Items           | 625.880 μs | 407.893 μs | 22.3580 μs | 73.94 |    2.41 | 9.7656 |  828626 B |       64.41 |
| Stream_WithPipelineBehaviors            |  41.737 μs |  15.194 μs |  0.8328 μs |  4.93 |    0.10 | 0.6104 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  66.171 μs |  45.985 μs |  2.5206 μs |  7.82 |    0.27 | 1.0986 |   96840 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  62.789 μs |  10.082 μs |  0.5526 μs |  7.42 |    0.09 | 0.9766 |   87024 B |        6.76 |
| Stream_WithCancellation_EarlyExit       |  84.632 μs |  36.298 μs |  1.9896 μs | 10.00 |    0.23 | 1.2207 |  102800 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.021 μs |   1.169 μs |  0.0641 μs |  0.36 |    0.01 | 0.0038 |     400 B |        0.03 |
