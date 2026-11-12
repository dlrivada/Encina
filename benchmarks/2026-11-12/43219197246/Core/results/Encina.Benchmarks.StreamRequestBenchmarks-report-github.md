```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.852 μs |   0.3872 μs | 0.0212 μs |  1.00 |    0.00 |  0.5035 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  90.948 μs |   8.1627 μs | 0.4474 μs |  7.08 |    0.03 |  3.4180 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 863.866 μs |  27.4027 μs | 1.5020 μs | 67.21 |    0.14 | 32.2266 |  828632 B |       64.41 |
| Stream_WithPipelineBehaviors            |  75.120 μs |   3.4376 μs | 0.1884 μs |  5.84 |    0.02 |  1.9531 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  93.681 μs |   3.3833 μs | 0.1855 μs |  7.29 |    0.02 |  3.7842 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  90.239 μs |   8.9192 μs | 0.4889 μs |  7.02 |    0.03 |  3.4180 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 137.659 μs | 117.4893 μs | 6.4400 μs | 10.71 |    0.43 |  3.9063 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.340 μs |   0.1260 μs | 0.0069 μs |  0.34 |    0.00 |  0.0153 |     400 B |        0.03 |
