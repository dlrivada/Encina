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
| Stream_SmallDataset_10Items             |  12.576 μs |  0.9898 μs | 0.0543 μs |  1.00 |    0.01 |  0.5035 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  87.576 μs |  7.8460 μs | 0.4301 μs |  6.96 |    0.04 |  3.4180 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 840.634 μs | 71.3933 μs | 3.9133 μs | 66.84 |    0.37 | 32.2266 |  828632 B |       64.41 |
| Stream_WithPipelineBehaviors            |  72.721 μs | 17.1655 μs | 0.9409 μs |  5.78 |    0.07 |  1.9531 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  90.400 μs | 25.9818 μs | 1.4241 μs |  7.19 |    0.10 |  3.7842 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  87.923 μs |  7.7624 μs | 0.4255 μs |  6.99 |    0.04 |  3.4180 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 129.648 μs |  9.1932 μs | 0.5039 μs | 10.31 |    0.05 |  3.9063 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.346 μs |  0.7419 μs | 0.0407 μs |  0.35 |    0.00 |  0.0153 |     400 B |        0.03 |
