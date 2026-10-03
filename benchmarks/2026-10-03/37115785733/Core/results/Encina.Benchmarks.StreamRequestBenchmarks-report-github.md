```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.029 μs |   0.6221 μs | 0.0341 μs |  1.00 |    0.00 |  0.5341 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  60.707 μs |   1.5701 μs | 0.0861 μs |  6.72 |    0.02 |  3.4790 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 552.798 μs | 116.5186 μs | 6.3868 μs | 61.23 |    0.64 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  49.949 μs |   3.3258 μs | 0.1823 μs |  5.53 |    0.03 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  62.750 μs |   6.3204 μs | 0.3464 μs |  6.95 |    0.04 |  4.0283 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  59.476 μs |   9.6281 μs | 0.5278 μs |  6.59 |    0.06 |  3.4790 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       |  97.289 μs |  11.2885 μs | 0.6188 μs | 10.78 |    0.07 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.107 μs |   0.4779 μs | 0.0262 μs |  0.45 |    0.00 |  0.0229 |      - |     400 B |        0.04 |
