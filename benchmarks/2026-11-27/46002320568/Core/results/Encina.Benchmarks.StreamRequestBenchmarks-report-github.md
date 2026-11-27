```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.320 μs |   1.4018 μs |  0.0768 μs |  1.00 |    0.01 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  65.742 μs |   3.6771 μs |  0.2016 μs |  7.05 |    0.05 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 608.371 μs | 246.4272 μs | 13.5075 μs | 65.28 |    1.34 | 48.8281 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  49.487 μs |   3.1455 μs |  0.1724 μs |  5.31 |    0.04 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  65.608 μs |  11.4975 μs |  0.6302 μs |  7.04 |    0.08 |  5.7373 |      - |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  63.562 μs |  12.3683 μs |  0.6780 μs |  6.82 |    0.08 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  98.809 μs |   8.8463 μs |  0.4849 μs | 10.60 |    0.09 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.427 μs |   0.7075 μs |  0.0388 μs |  0.37 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
