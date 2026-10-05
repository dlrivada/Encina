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
| Stream_SmallDataset_10Items             |   9.992 μs |  3.0963 μs | 0.1697 μs |  1.00 |    0.02 |  0.5341 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  64.508 μs | 12.5422 μs | 0.6875 μs |  6.46 |    0.11 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 620.922 μs | 73.1415 μs | 4.0091 μs | 62.15 |    0.98 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  57.451 μs |  1.1116 μs | 0.0609 μs |  5.75 |    0.08 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  69.347 μs |  8.7356 μs | 0.4788 μs |  6.94 |    0.11 |  4.0283 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  64.965 μs |  6.3840 μs | 0.3499 μs |  6.50 |    0.10 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       | 109.692 μs | 14.2056 μs | 0.7787 μs | 10.98 |    0.17 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.107 μs |  0.6699 μs | 0.0367 μs |  0.41 |    0.01 |  0.0229 |      - |     400 B |        0.04 |
