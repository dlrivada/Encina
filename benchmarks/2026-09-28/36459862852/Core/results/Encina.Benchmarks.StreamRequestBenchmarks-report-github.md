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
| Stream_SmallDataset_10Items             |   9.897 μs |  1.2329 μs | 0.0676 μs |  1.00 |    0.01 |  0.5341 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  64.130 μs |  2.6037 μs | 0.1427 μs |  6.48 |    0.04 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 662.268 μs | 78.0221 μs | 4.2767 μs | 66.92 |    0.54 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  56.840 μs |  3.5339 μs | 0.1937 μs |  5.74 |    0.04 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  67.664 μs |  6.9803 μs | 0.3826 μs |  6.84 |    0.05 |  4.0283 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  65.584 μs |  2.8851 μs | 0.1581 μs |  6.63 |    0.04 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       | 108.076 μs |  7.2676 μs | 0.3984 μs | 10.92 |    0.07 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.147 μs |  0.9139 μs | 0.0501 μs |  0.42 |    0.01 |  0.0229 |      - |     400 B |        0.04 |
