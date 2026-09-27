```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.836 μs |  1.7217 μs | 0.0944 μs |  1.00 |    0.01 |  0.5341 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  64.045 μs |  1.5123 μs | 0.0829 μs |  6.51 |    0.05 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 603.682 μs | 70.8980 μs | 3.8862 μs | 61.38 |    0.62 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  56.185 μs |  5.5147 μs | 0.3023 μs |  5.71 |    0.05 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  67.948 μs |  5.5353 μs | 0.3034 μs |  6.91 |    0.06 |  4.0283 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  63.902 μs |  2.4883 μs | 0.1364 μs |  6.50 |    0.06 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       | 109.842 μs | 24.5662 μs | 1.3466 μs | 11.17 |    0.15 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.089 μs |  0.7052 μs | 0.0387 μs |  0.42 |    0.00 |  0.0229 |      - |     400 B |        0.04 |
