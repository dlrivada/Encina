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
| Stream_SmallDataset_10Items             |  12.548 μs |  0.3038 μs | 0.0167 μs |  1.00 |    0.00 |  0.5035 |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  91.680 μs | 37.3509 μs | 2.0473 μs |  7.31 |    0.14 |  3.4180 |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 874.910 μs | 83.7957 μs | 4.5931 μs | 69.72 |    0.33 | 32.2266 |  828624 B |       64.45 |
| Stream_WithPipelineBehaviors            |  71.459 μs |  3.7943 μs | 0.2080 μs |  5.69 |    0.02 |  1.9531 |   51544 B |        4.01 |
| Stream_MaterializeToList_100Items       |  96.567 μs | 10.6054 μs | 0.5813 μs |  7.70 |    0.04 |  3.7842 |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  90.647 μs |  5.9067 μs | 0.3238 μs |  7.22 |    0.02 |  3.4180 |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 131.354 μs |  2.6624 μs | 0.1459 μs | 10.47 |    0.02 |  3.9063 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   4.317 μs |  0.4037 μs | 0.0221 μs |  0.34 |    0.00 |  0.0153 |     400 B |        0.03 |
