```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.311 μs |  0.6841 μs | 0.0375 μs |  1.00 |    0.00 |  0.5035 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  87.205 μs |  2.6928 μs | 0.1476 μs |  7.08 |    0.02 |  3.4180 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 845.103 μs | 57.0183 μs | 3.1254 μs | 68.65 |    0.28 | 32.2266 |  828632 B |       64.41 |
| Stream_WithPipelineBehaviors            |  71.300 μs |  4.9335 μs | 0.2704 μs |  5.79 |    0.02 |  1.9531 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  90.275 μs | 17.1161 μs | 0.9382 μs |  7.33 |    0.07 |  3.7842 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  87.209 μs |  5.5292 μs | 0.3031 μs |  7.08 |    0.03 |  3.4180 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 128.097 μs |  2.6237 μs | 0.1438 μs | 10.41 |    0.03 |  3.9063 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.117 μs |  0.1072 μs | 0.0059 μs |  0.33 |    0.00 |  0.0153 |     400 B |        0.03 |
