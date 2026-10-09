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
| Stream_SmallDataset_10Items             |  11.911 μs |  1.0747 μs | 0.0589 μs |  1.00 |    0.01 |  0.7629 |      - |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  81.197 μs | 11.2045 μs | 0.6142 μs |  6.82 |    0.05 |  5.1270 |      - |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 761.624 μs | 43.5114 μs | 2.3850 μs | 63.95 |    0.32 | 48.8281 |      - |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  64.552 μs |  5.1392 μs | 0.2817 μs |  5.42 |    0.03 |  3.0518 |      - |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  86.282 μs | 15.1953 μs | 0.8329 μs |  7.24 |    0.07 |  5.7373 |      - |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  84.437 μs |  6.3485 μs | 0.3480 μs |  7.09 |    0.04 |  5.1270 |      - |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 123.034 μs |  3.3071 μs | 0.1813 μs | 10.33 |    0.05 |  6.1035 | 0.1221 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   4.320 μs |  0.2351 μs | 0.0129 μs |  0.36 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
