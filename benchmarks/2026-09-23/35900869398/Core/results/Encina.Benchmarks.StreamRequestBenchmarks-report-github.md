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
| Stream_SmallDataset_10Items             |   9.691 μs |  1.6920 μs | 0.0927 μs |  1.00 |    0.01 |  0.5341 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  64.292 μs | 10.6613 μs | 0.5844 μs |  6.63 |    0.08 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 615.220 μs | 33.3071 μs | 1.8257 μs | 63.49 |    0.55 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  56.479 μs |  2.8619 μs | 0.1569 μs |  5.83 |    0.05 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  67.507 μs |  1.0710 μs | 0.0587 μs |  6.97 |    0.06 |  4.0283 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  63.984 μs |  6.3598 μs | 0.3486 μs |  6.60 |    0.06 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       | 107.549 μs |  0.4390 μs | 0.0241 μs | 11.10 |    0.09 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.134 μs |  0.5649 μs | 0.0310 μs |  0.43 |    0.00 |  0.0229 |      - |     400 B |        0.04 |
