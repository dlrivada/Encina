```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.98GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                  | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.823 μs | 0.0468 μs | 0.0701 μs |   9.815 μs |  1.00 |    0.01 |  0.5341 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  64.924 μs | 0.4509 μs | 0.6467 μs |  65.086 μs |  6.61 |    0.08 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 599.670 μs | 4.3877 μs | 6.4314 μs | 603.073 μs | 61.05 |    0.77 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  58.595 μs | 1.5693 μs | 2.3489 μs |  58.608 μs |  5.97 |    0.24 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  67.728 μs | 0.3600 μs | 0.5388 μs |  67.872 μs |  6.89 |    0.07 |  4.0283 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  65.183 μs | 0.1554 μs | 0.2326 μs |  65.140 μs |  6.64 |    0.05 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       | 109.111 μs | 0.7420 μs | 1.0876 μs | 108.926 μs | 11.11 |    0.13 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.098 μs | 0.0256 μs | 0.0367 μs |   4.099 μs |  0.42 |    0.00 |  0.0229 |      - |     400 B |        0.04 |
