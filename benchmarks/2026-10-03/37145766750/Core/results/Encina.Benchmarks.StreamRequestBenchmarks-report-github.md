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
| Stream_SmallDataset_10Items             |   9.575 μs |  1.2683 μs | 0.0695 μs |  1.00 |    0.01 |  0.5341 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  64.175 μs |  4.9110 μs | 0.2692 μs |  6.70 |    0.05 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 606.705 μs | 41.2385 μs | 2.2604 μs | 63.37 |    0.45 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  55.787 μs |  5.9972 μs | 0.3287 μs |  5.83 |    0.05 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  71.724 μs |  2.9410 μs | 0.1612 μs |  7.49 |    0.05 |  4.0283 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  64.382 μs |  6.6394 μs | 0.3639 μs |  6.72 |    0.05 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       | 109.983 μs | 19.0278 μs | 1.0430 μs | 11.49 |    0.12 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.067 μs |  0.2559 μs | 0.0140 μs |  0.42 |    0.00 |  0.0229 |      - |     400 B |        0.04 |
