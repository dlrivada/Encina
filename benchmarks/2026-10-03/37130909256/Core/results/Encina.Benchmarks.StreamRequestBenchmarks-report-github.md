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
| Stream_SmallDataset_10Items             |   9.683 μs |  0.6212 μs | 0.0341 μs |  1.00 |    0.00 |  0.5341 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  63.205 μs |  4.4560 μs | 0.2442 μs |  6.53 |    0.03 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 605.150 μs | 48.3540 μs | 2.6504 μs | 62.50 |    0.30 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  56.298 μs |  9.4357 μs | 0.5172 μs |  5.81 |    0.05 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  68.323 μs |  5.3229 μs | 0.2918 μs |  7.06 |    0.03 |  4.0283 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  63.802 μs |  6.5166 μs | 0.3572 μs |  6.59 |    0.04 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       | 109.122 μs |  3.9500 μs | 0.2165 μs | 11.27 |    0.04 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.109 μs |  0.2587 μs | 0.0142 μs |  0.42 |    0.00 |  0.0229 |      - |     400 B |        0.04 |
