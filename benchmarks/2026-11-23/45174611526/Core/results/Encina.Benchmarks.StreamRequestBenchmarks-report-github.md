```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.98GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   8.437 μs |   8.9581 μs |  0.4910 μs |  1.00 |    0.07 | 0.1526 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  57.264 μs |  42.0437 μs |  2.3046 μs |  6.80 |    0.43 | 1.0376 |   87024 B |        6.76 |
| Stream_LargeDataset_1000Items           | 561.983 μs | 320.2818 μs | 17.5557 μs | 66.77 |    3.92 | 9.7656 |  828626 B |       64.41 |
| Stream_WithPipelineBehaviors            |  40.688 μs |  10.7945 μs |  0.5917 μs |  4.83 |    0.26 | 0.6104 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  60.786 μs |  60.3139 μs |  3.3060 μs |  7.22 |    0.51 | 1.0986 |   96840 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  58.580 μs |  41.6676 μs |  2.2839 μs |  6.96 |    0.43 | 1.0376 |   87024 B |        6.76 |
| Stream_WithCancellation_EarlyExit       |  80.619 μs |   7.3354 μs |  0.4021 μs |  9.58 |    0.50 | 1.2207 |  102800 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   2.884 μs |   0.0777 μs |  0.0043 μs |  0.34 |    0.02 | 0.0038 |     400 B |        0.03 |
