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
| Stream_SmallDataset_10Items             |   9.892 μs |  2.7613 μs | 0.1514 μs |  1.00 |    0.02 |  0.5341 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  65.120 μs |  2.1125 μs | 0.1158 μs |  6.58 |    0.09 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 617.134 μs | 65.8993 μs | 3.6122 μs | 62.40 |    0.88 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  56.915 μs |  2.3856 μs | 0.1308 μs |  5.75 |    0.08 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  68.213 μs |  6.7179 μs | 0.3682 μs |  6.90 |    0.10 |  4.0283 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  65.020 μs | 29.4716 μs | 1.6154 μs |  6.57 |    0.17 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       | 108.207 μs |  8.4263 μs | 0.4619 μs | 10.94 |    0.15 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.171 μs |  0.2998 μs | 0.0164 μs |  0.42 |    0.01 |  0.0229 |      - |     400 B |        0.04 |
