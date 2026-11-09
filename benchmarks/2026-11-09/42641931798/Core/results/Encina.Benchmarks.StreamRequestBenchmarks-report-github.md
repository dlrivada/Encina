```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   7.382 μs |   5.6064 μs |  0.3073 μs |  1.00 |    0.05 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  43.950 μs |   1.5332 μs |  0.0840 μs |  5.96 |    0.21 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 432.861 μs | 316.9653 μs | 17.3739 μs | 58.70 |    2.91 | 49.3164 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  40.764 μs |  14.6865 μs |  0.8050 μs |  5.53 |    0.22 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  49.421 μs |  40.7522 μs |  2.2338 μs |  6.70 |    0.35 |  5.7373 | 0.0610 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  45.423 μs |  24.3876 μs |  1.3368 μs |  6.16 |    0.27 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  70.451 μs |  55.6180 μs |  3.0486 μs |  9.55 |    0.49 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   2.327 μs |   0.4993 μs |  0.0274 μs |  0.32 |    0.01 |  0.0229 |      - |     400 B |        0.03 |
