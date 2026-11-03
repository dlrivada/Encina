```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   6.593 μs |  1.1056 μs | 0.0606 μs |  1.00 |    0.01 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  41.432 μs |  8.3831 μs | 0.4595 μs |  6.28 |    0.08 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 439.905 μs | 38.1932 μs | 2.0935 μs | 66.72 |    0.60 | 49.3164 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  37.405 μs |  7.8610 μs | 0.4309 μs |  5.67 |    0.07 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  43.294 μs |  2.9414 μs | 0.1612 μs |  6.57 |    0.06 |  5.7373 | 0.0610 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  42.019 μs |  7.8524 μs | 0.4304 μs |  6.37 |    0.08 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  65.351 μs |  7.9946 μs | 0.4382 μs |  9.91 |    0.10 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   2.221 μs |  0.0935 μs | 0.0051 μs |  0.34 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
