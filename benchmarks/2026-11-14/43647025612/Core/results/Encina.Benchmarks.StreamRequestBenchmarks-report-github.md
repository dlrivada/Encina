```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.582 μs |  0.9449 μs | 0.0518 μs |  1.00 |    0.01 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  82.561 μs | 18.6989 μs | 1.0250 μs |  6.56 |    0.07 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 804.889 μs | 52.2093 μs | 2.8618 μs | 63.97 |    0.30 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  68.212 μs | 12.6370 μs | 0.6927 μs |  5.42 |    0.05 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  89.138 μs |  3.8977 μs | 0.2136 μs |  7.08 |    0.03 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  82.336 μs |  6.7809 μs | 0.3717 μs |  6.54 |    0.03 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 128.219 μs |  4.4265 μs | 0.2426 μs | 10.19 |    0.04 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.040 μs |  0.5604 μs | 0.0307 μs |  0.32 |    0.00 |  0.0229 |     400 B |        0.03 |
