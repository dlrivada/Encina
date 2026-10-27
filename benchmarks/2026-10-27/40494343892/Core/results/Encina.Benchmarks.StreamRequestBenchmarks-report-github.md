```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   6.345 μs |  0.3946 μs | 0.0216 μs |  1.00 |    0.00 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  41.817 μs |  3.7350 μs | 0.2047 μs |  6.59 |    0.03 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 393.224 μs | 11.2908 μs | 0.6189 μs | 61.98 |    0.20 | 49.3164 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  36.867 μs |  1.9765 μs | 0.1083 μs |  5.81 |    0.02 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  42.482 μs |  1.6335 μs | 0.0895 μs |  6.70 |    0.02 |  5.7373 | 0.0610 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  41.881 μs |  2.0197 μs | 0.1107 μs |  6.60 |    0.02 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  62.966 μs |  5.1438 μs | 0.2820 μs |  9.92 |    0.05 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   2.261 μs |  0.1040 μs | 0.0057 μs |  0.36 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
