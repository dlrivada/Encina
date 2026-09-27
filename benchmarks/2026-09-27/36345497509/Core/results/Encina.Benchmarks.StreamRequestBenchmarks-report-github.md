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
| Stream_SmallDataset_10Items             |   9.822 μs |  1.2482 μs | 0.0684 μs |  1.00 |    0.01 |  0.5341 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  66.115 μs |  2.9591 μs | 0.1622 μs |  6.73 |    0.04 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 613.116 μs | 31.5280 μs | 1.7282 μs | 62.42 |    0.41 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  59.938 μs |  1.8114 μs | 0.0993 μs |  6.10 |    0.04 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  69.750 μs |  5.8558 μs | 0.3210 μs |  7.10 |    0.05 |  4.0283 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  64.096 μs |  5.6618 μs | 0.3103 μs |  6.53 |    0.05 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       | 110.029 μs |  4.6268 μs | 0.2536 μs | 11.20 |    0.07 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.037 μs |  0.5010 μs | 0.0275 μs |  0.41 |    0.00 |  0.0229 |      - |     400 B |        0.04 |
