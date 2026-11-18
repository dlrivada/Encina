```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.96GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.334 μs |  1.1977 μs | 0.0657 μs |  1.00 |    0.01 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  86.818 μs | 25.8730 μs | 1.4182 μs |  7.04 |    0.10 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 778.621 μs | 18.2452 μs | 1.0001 μs | 63.13 |    0.30 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  66.989 μs | 28.7166 μs | 1.5741 μs |  5.43 |    0.11 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  88.516 μs | 21.7229 μs | 1.1907 μs |  7.18 |    0.09 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  81.583 μs |  3.7996 μs | 0.2083 μs |  6.61 |    0.03 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 124.360 μs |  9.0189 μs | 0.4944 μs | 10.08 |    0.06 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.034 μs |  0.6922 μs | 0.0379 μs |  0.33 |    0.00 |  0.0229 |     400 B |        0.03 |
