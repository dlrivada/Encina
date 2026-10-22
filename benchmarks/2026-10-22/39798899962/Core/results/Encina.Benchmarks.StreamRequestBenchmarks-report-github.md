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
| Stream_SmallDataset_10Items             |  12.713 μs |  1.2685 μs | 0.0695 μs |  1.00 |    0.01 |  0.5035 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  90.436 μs |  0.6966 μs | 0.0382 μs |  7.11 |    0.03 |  3.4180 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 870.385 μs | 28.4387 μs | 1.5588 μs | 68.47 |    0.34 | 32.2266 |  828632 B |       64.41 |
| Stream_WithPipelineBehaviors            |  71.103 μs |  2.1793 μs | 0.1195 μs |  5.59 |    0.03 |  1.9531 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  94.052 μs |  3.5254 μs | 0.1932 μs |  7.40 |    0.04 |  3.7842 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  92.375 μs |  0.6282 μs | 0.0344 μs |  7.27 |    0.03 |  3.4180 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 127.542 μs |  4.0915 μs | 0.2243 μs | 10.03 |    0.05 |  3.9063 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.424 μs |  1.3395 μs | 0.0734 μs |  0.35 |    0.01 |  0.0153 |     400 B |        0.03 |
