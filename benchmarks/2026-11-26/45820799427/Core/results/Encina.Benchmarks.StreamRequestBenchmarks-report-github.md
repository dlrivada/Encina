```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.196 μs |  0.6624 μs | 0.0363 μs |  1.00 |    0.00 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  82.472 μs |  4.6865 μs | 0.2569 μs |  6.76 |    0.03 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 779.630 μs | 38.2699 μs | 2.0977 μs | 63.93 |    0.22 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  69.145 μs | 34.0017 μs | 1.8637 μs |  5.67 |    0.13 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  89.241 μs |  1.7327 μs | 0.0950 μs |  7.32 |    0.02 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  80.392 μs | 11.2396 μs | 0.6161 μs |  6.59 |    0.05 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 126.397 μs | 16.3011 μs | 0.8935 μs | 10.36 |    0.07 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.036 μs |  0.4707 μs | 0.0258 μs |  0.33 |    0.00 |  0.0229 |     400 B |        0.03 |
