```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.105 μs |  0.4851 μs | 0.0266 μs |  1.00 |    0.00 |  0.7629 |      - |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  61.987 μs |  1.0299 μs | 0.0565 μs |  6.81 |    0.02 |  5.1270 |      - |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 576.482 μs | 38.4808 μs | 2.1093 μs | 63.31 |    0.26 | 48.8281 |      - |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  49.061 μs |  7.0556 μs | 0.3867 μs |  5.39 |    0.04 |  3.0518 |      - |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  64.359 μs |  5.9713 μs | 0.3273 μs |  7.07 |    0.04 |  5.7373 |      - |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  61.738 μs |  4.7851 μs | 0.2623 μs |  6.78 |    0.03 |  5.1270 |      - |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  91.872 μs |  8.3208 μs | 0.4561 μs | 10.09 |    0.05 |  6.1035 | 0.1221 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   3.449 μs |  0.7330 μs | 0.0402 μs |  0.38 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
