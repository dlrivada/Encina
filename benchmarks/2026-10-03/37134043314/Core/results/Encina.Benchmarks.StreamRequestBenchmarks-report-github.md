```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.688 μs |  2.4261 μs | 0.1330 μs |  1.00 |    0.02 |  0.5341 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  63.816 μs |  4.8276 μs | 0.2646 μs |  6.59 |    0.08 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 610.072 μs | 60.2161 μs | 3.3006 μs | 62.98 |    0.80 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  55.405 μs |  2.6169 μs | 0.1434 μs |  5.72 |    0.07 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  68.254 μs |  4.8084 μs | 0.2636 μs |  7.05 |    0.09 |  4.0283 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  65.567 μs |  4.0915 μs | 0.2243 μs |  6.77 |    0.08 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       | 107.449 μs |  4.0481 μs | 0.2219 μs | 11.09 |    0.13 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.286 μs |  0.9569 μs | 0.0525 μs |  0.44 |    0.01 |  0.0229 |      - |     400 B |        0.04 |
