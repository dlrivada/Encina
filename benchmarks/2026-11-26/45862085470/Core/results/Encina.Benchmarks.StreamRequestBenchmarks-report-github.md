```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.58GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.892 μs |   1.1431 μs | 0.0627 μs |  1.00 |    0.01 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  68.710 μs |   3.1886 μs | 0.1748 μs |  6.95 |    0.04 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 617.876 μs | 115.1120 μs | 6.3097 μs | 62.46 |    0.65 | 48.8281 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  53.017 μs |   9.7768 μs | 0.5359 μs |  5.36 |    0.06 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  69.415 μs |   6.3925 μs | 0.3504 μs |  7.02 |    0.05 |  5.7373 |      - |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  70.602 μs |  11.8739 μs | 0.6508 μs |  7.14 |    0.07 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  97.379 μs |   9.0290 μs | 0.4949 μs |  9.84 |    0.07 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.415 μs |   0.8948 μs | 0.0490 μs |  0.35 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
