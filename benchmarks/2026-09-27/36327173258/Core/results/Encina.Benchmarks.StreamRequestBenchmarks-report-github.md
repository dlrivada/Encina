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
| Stream_SmallDataset_10Items             |   9.666 μs |  0.5939 μs | 0.0326 μs |  1.00 |    0.00 |  0.5341 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  64.273 μs |  1.8384 μs | 0.1008 μs |  6.65 |    0.02 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 595.031 μs | 72.0158 μs | 3.9474 μs | 61.56 |    0.40 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  56.414 μs |  4.2153 μs | 0.2311 μs |  5.84 |    0.03 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  67.033 μs |  3.1858 μs | 0.1746 μs |  6.93 |    0.03 |  4.0283 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  63.833 μs |  4.0940 μs | 0.2244 μs |  6.60 |    0.03 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       | 109.733 μs |  0.7825 μs | 0.0429 μs | 11.35 |    0.03 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.080 μs |  0.2769 μs | 0.0152 μs |  0.42 |    0.00 |  0.0229 |      - |     400 B |        0.04 |
