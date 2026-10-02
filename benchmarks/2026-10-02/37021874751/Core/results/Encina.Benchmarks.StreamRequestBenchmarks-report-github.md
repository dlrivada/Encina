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
| Stream_SmallDataset_10Items             |   9.960 μs |  0.5460 μs | 0.0299 μs |  1.00 |    0.00 |  0.5341 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  66.116 μs |  9.0940 μs | 0.4985 μs |  6.64 |    0.05 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 619.156 μs | 23.8655 μs | 1.3081 μs | 62.16 |    0.20 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  57.622 μs |  8.0263 μs | 0.4399 μs |  5.79 |    0.04 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  69.832 μs |  5.3561 μs | 0.2936 μs |  7.01 |    0.03 |  4.0283 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  65.131 μs |  5.7174 μs | 0.3134 μs |  6.54 |    0.03 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       | 110.677 μs | 24.0413 μs | 1.3178 μs | 11.11 |    0.12 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.099 μs |  0.3465 μs | 0.0190 μs |  0.41 |    0.00 |  0.0229 |      - |     400 B |        0.04 |
