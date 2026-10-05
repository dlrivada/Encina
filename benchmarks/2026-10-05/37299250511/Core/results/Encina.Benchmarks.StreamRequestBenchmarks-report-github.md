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
| Stream_SmallDataset_10Items             |   9.704 μs |  2.4263 μs | 0.1330 μs |  1.00 |    0.02 |  0.5341 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  65.161 μs |  1.6762 μs | 0.0919 μs |  6.72 |    0.08 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 620.764 μs | 55.9349 μs | 3.0660 μs | 63.98 |    0.80 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  56.218 μs |  2.6343 μs | 0.1444 μs |  5.79 |    0.07 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  66.209 μs |  0.9397 μs | 0.0515 μs |  6.82 |    0.08 |  4.0283 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  63.116 μs |  6.9358 μs | 0.3802 μs |  6.51 |    0.08 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       | 107.291 μs |  2.1231 μs | 0.1164 μs | 11.06 |    0.13 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.046 μs |  0.6261 μs | 0.0343 μs |  0.42 |    0.01 |  0.0229 |      - |     400 B |        0.04 |
