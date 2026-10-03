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
| Stream_SmallDataset_10Items             |   5.302 μs |   2.0556 μs | 0.1127 μs |  1.00 |    0.03 |  0.5417 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  34.111 μs |   8.8578 μs | 0.4855 μs |  6.44 |    0.14 |  3.4790 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 316.128 μs | 146.6954 μs | 8.0409 μs | 59.64 |    1.71 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  32.003 μs |   3.8527 μs | 0.2112 μs |  6.04 |    0.12 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  35.137 μs |   0.8643 μs | 0.0474 μs |  6.63 |    0.12 |  4.0894 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  33.425 μs |   3.1100 μs | 0.1705 μs |  6.31 |    0.12 |  3.4790 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       |  56.637 μs |   8.0639 μs | 0.4420 μs | 10.69 |    0.21 |  4.4556 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   2.315 μs |   0.0509 μs | 0.0028 μs |  0.44 |    0.01 |  0.0229 |      - |     400 B |        0.04 |
