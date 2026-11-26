```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.661 μs |  0.2103 μs | 0.0115 μs |  1.00 |    0.00 |  0.5035 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  88.506 μs |  5.3292 μs | 0.2921 μs |  6.99 |    0.02 |  3.4180 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 849.923 μs | 13.8036 μs | 0.7566 μs | 67.13 |    0.07 | 32.2266 |  828632 B |       64.41 |
| Stream_WithPipelineBehaviors            |  75.228 μs |  1.4675 μs | 0.0804 μs |  5.94 |    0.01 |  1.9531 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  91.621 μs |  1.2210 μs | 0.0669 μs |  7.24 |    0.01 |  3.7842 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  88.779 μs |  6.7054 μs | 0.3675 μs |  7.01 |    0.03 |  3.4180 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 128.835 μs |  2.4646 μs | 0.1351 μs | 10.18 |    0.01 |  3.9063 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.222 μs |  0.2369 μs | 0.0130 μs |  0.33 |    0.00 |  0.0153 |     400 B |        0.03 |
