```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   6.204 μs |   0.7450 μs | 0.0408 μs |  1.00 |    0.01 |  0.7629 |      - |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  39.987 μs |   4.6974 μs | 0.2575 μs |  6.45 |    0.05 |  5.1880 |      - |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 378.677 μs | 149.2951 μs | 8.1834 μs | 61.04 |    1.19 | 49.3164 | 0.4883 |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  34.372 μs |   4.3394 μs | 0.2379 μs |  5.54 |    0.05 |  3.0518 |      - |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  42.408 μs |  30.8838 μs | 1.6928 μs |  6.84 |    0.24 |  5.7373 | 0.0610 |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  42.644 μs |  21.0860 μs | 1.1558 μs |  6.87 |    0.17 |  5.1880 |      - |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  60.983 μs |   4.6555 μs | 0.2552 μs |  9.83 |    0.07 |  6.1035 | 0.1831 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   2.244 μs |   0.3701 μs | 0.0203 μs |  0.36 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
