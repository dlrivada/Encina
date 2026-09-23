```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.753 μs |   1.003 μs |  0.0550 μs |  1.00 |    0.01 |  0.5341 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  64.126 μs |   7.803 μs |  0.4277 μs |  6.58 |    0.05 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 624.377 μs | 367.692 μs | 20.1544 μs | 64.02 |    1.82 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  57.235 μs |   4.866 μs |  0.2667 μs |  5.87 |    0.04 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  70.465 μs |  76.437 μs |  4.1898 μs |  7.23 |    0.37 |  4.0283 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  63.916 μs |   9.547 μs |  0.5233 μs |  6.55 |    0.06 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       | 108.138 μs |  13.383 μs |  0.7336 μs | 11.09 |    0.08 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.547 μs |   2.017 μs |  0.1106 μs |  0.47 |    0.01 |  0.0229 |      - |     400 B |        0.04 |
