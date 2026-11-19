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
| Stream_SmallDataset_10Items             |  12.514 μs |  0.8944 μs | 0.0490 μs |  1.00 |    0.00 |  0.5035 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  87.272 μs |  5.7836 μs | 0.3170 μs |  6.97 |    0.03 |  3.4180 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 841.371 μs | 25.5791 μs | 1.4021 μs | 67.23 |    0.25 | 32.2266 |  828632 B |       64.41 |
| Stream_WithPipelineBehaviors            |  74.304 μs |  7.5130 μs | 0.4118 μs |  5.94 |    0.03 |  1.9531 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  90.800 μs |  5.7354 μs | 0.3144 μs |  7.26 |    0.03 |  3.7842 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  87.829 μs |  3.2039 μs | 0.1756 μs |  7.02 |    0.03 |  3.4180 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 131.609 μs | 31.1271 μs | 1.7062 μs | 10.52 |    0.12 |  3.9063 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.300 μs |  0.1063 μs | 0.0058 μs |  0.34 |    0.00 |  0.0153 |     400 B |        0.03 |
