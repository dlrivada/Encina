```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.289 μs |  0.1665 μs | 0.0091 μs |  1.00 |    0.00 |  0.7629 |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  83.652 μs | 13.6058 μs | 0.7458 μs |  6.81 |    0.05 |  5.1270 |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 781.686 μs | 18.6504 μs | 1.0223 μs | 63.61 |    0.08 | 48.8281 |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  68.408 μs |  6.2906 μs | 0.3448 μs |  5.57 |    0.02 |  3.0518 |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  89.928 μs | 14.3913 μs | 0.7888 μs |  7.32 |    0.06 |  5.7373 |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  83.173 μs |  4.9449 μs | 0.2710 μs |  6.77 |    0.02 |  5.1270 |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 127.204 μs |  5.9963 μs | 0.3287 μs | 10.35 |    0.02 |  6.1035 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   4.193 μs |  0.7414 μs | 0.0406 μs |  0.34 |    0.00 |  0.0229 |     400 B |        0.03 |
