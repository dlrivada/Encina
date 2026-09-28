```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   5.327 μs |  0.5628 μs | 0.0309 μs |  1.00 |    0.01 |  0.5417 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  32.972 μs |  2.0454 μs | 0.1121 μs |  6.19 |    0.04 |  3.4790 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 315.903 μs | 20.5130 μs | 1.1244 μs | 59.30 |    0.35 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  31.224 μs |  2.9414 μs | 0.1612 μs |  5.86 |    0.04 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  34.510 μs |  4.6733 μs | 0.2562 μs |  6.48 |    0.05 |  4.0894 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  31.639 μs |  4.5532 μs | 0.2496 μs |  5.94 |    0.05 |  3.4790 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       |  55.346 μs |  3.2605 μs | 0.1787 μs | 10.39 |    0.06 |  4.4556 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   2.292 μs |  0.0940 μs | 0.0052 μs |  0.43 |    0.00 |  0.0229 |      - |     400 B |        0.04 |
