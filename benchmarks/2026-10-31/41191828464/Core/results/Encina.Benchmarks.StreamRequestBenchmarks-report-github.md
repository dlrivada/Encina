```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.113 μs |  0.3310 μs | 0.0181 μs |  1.00 |    0.00 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  80.117 μs |  3.9945 μs | 0.2190 μs |  6.61 |    0.02 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 766.270 μs | 97.3355 μs | 5.3353 μs | 63.26 |    0.39 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  71.348 μs |  7.0771 μs | 0.3879 μs |  5.89 |    0.03 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  84.929 μs |  5.7320 μs | 0.3142 μs |  7.01 |    0.02 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  82.355 μs | 11.2001 μs | 0.6139 μs |  6.80 |    0.04 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 134.407 μs |  2.3143 μs | 0.1269 μs | 11.10 |    0.02 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.122 μs |  0.9314 μs | 0.0511 μs |  0.34 |    0.00 |  0.0229 |     400 B |        0.03 |
