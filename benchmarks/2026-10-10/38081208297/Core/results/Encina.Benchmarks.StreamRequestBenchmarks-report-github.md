```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   6.490 μs |   2.3328 μs | 0.1279 μs |  1.00 |    0.02 |  0.7629 |      - |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  42.102 μs |   7.3669 μs | 0.4038 μs |  6.49 |    0.12 |  5.1880 |      - |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 394.761 μs | 143.1354 μs | 7.8457 μs | 60.85 |    1.47 | 49.3164 | 0.4883 |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  36.339 μs |   4.8492 μs | 0.2658 μs |  5.60 |    0.10 |  3.0518 |      - |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  43.316 μs |   5.8763 μs | 0.3221 μs |  6.68 |    0.12 |  5.7373 | 0.0610 |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  44.243 μs |  29.4841 μs | 1.6161 μs |  6.82 |    0.24 |  5.1880 |      - |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  64.144 μs |   0.5357 μs | 0.0294 μs |  9.89 |    0.17 |  6.1035 | 0.1221 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   2.320 μs |   0.4153 μs | 0.0228 μs |  0.36 |    0.01 |  0.0229 |      - |     400 B |        0.03 |
