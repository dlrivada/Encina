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
| Stream_SmallDataset_10Items             |   9.660 μs |  1.2202 μs | 0.0669 μs |  1.00 |    0.01 |  0.5341 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  64.760 μs |  7.6349 μs | 0.4185 μs |  6.70 |    0.06 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 607.058 μs | 78.6752 μs | 4.3125 μs | 62.85 |    0.54 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  59.252 μs | 38.2968 μs | 2.0992 μs |  6.13 |    0.19 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  70.311 μs |  9.9904 μs | 0.5476 μs |  7.28 |    0.07 |  4.0283 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  63.425 μs |  5.6294 μs | 0.3086 μs |  6.57 |    0.05 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       | 108.397 μs |  5.6963 μs | 0.3122 μs | 11.22 |    0.07 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.142 μs |  0.7841 μs | 0.0430 μs |  0.43 |    0.00 |  0.0229 |      - |     400 B |        0.04 |
