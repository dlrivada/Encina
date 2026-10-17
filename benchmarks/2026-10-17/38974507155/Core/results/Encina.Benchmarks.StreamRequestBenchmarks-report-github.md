```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.482 μs |  0.0773 μs | 0.0042 μs |  1.00 |    0.00 |  0.5035 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  91.406 μs |  2.4012 μs | 0.1316 μs |  7.32 |    0.01 |  3.4180 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 849.447 μs | 34.5523 μs | 1.8939 μs | 68.05 |    0.13 | 32.2266 |  828632 B |       64.41 |
| Stream_WithPipelineBehaviors            |  71.618 μs |  2.1046 μs | 0.1154 μs |  5.74 |    0.01 |  1.9531 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  92.279 μs |  2.6764 μs | 0.1467 μs |  7.39 |    0.01 |  3.7842 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  88.047 μs |  1.0345 μs | 0.0567 μs |  7.05 |    0.00 |  3.4180 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 130.996 μs |  1.9772 μs | 0.1084 μs | 10.49 |    0.01 |  3.9063 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.147 μs |  0.2482 μs | 0.0136 μs |  0.33 |    0.00 |  0.0153 |     400 B |        0.03 |
