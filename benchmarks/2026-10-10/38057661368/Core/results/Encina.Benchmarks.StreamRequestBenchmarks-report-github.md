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
| Stream_SmallDataset_10Items             |   6.788 μs |   1.9944 μs | 0.1093 μs |  1.00 |    0.02 |  0.7629 |      - |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  44.987 μs |   6.4883 μs | 0.3556 μs |  6.63 |    0.10 |  5.1880 |      - |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 424.757 μs | 173.7310 μs | 9.5228 μs | 62.59 |    1.49 | 49.3164 | 0.4883 |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  38.231 μs |   5.2422 μs | 0.2873 μs |  5.63 |    0.09 |  3.0518 |      - |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  47.146 μs |  21.7026 μs | 1.1896 μs |  6.95 |    0.18 |  5.7373 | 0.0610 |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  44.525 μs |   2.6433 μs | 0.1449 μs |  6.56 |    0.09 |  5.1880 |      - |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  68.431 μs |  36.1910 μs | 1.9838 μs | 10.08 |    0.29 |  6.1035 | 0.1221 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   2.346 μs |   0.4199 μs | 0.0230 μs |  0.35 |    0.01 |  0.0229 |      - |     400 B |        0.03 |
