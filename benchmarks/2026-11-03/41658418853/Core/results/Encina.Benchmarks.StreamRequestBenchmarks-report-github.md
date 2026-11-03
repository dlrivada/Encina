```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.636 μs |  1.010 μs | 0.0553 μs |  1.00 |    0.01 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  84.301 μs | 10.997 μs | 0.6028 μs |  6.67 |    0.05 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 806.469 μs | 84.808 μs | 4.6486 μs | 63.82 |    0.40 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  68.104 μs |  1.932 μs | 0.1059 μs |  5.39 |    0.02 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  91.719 μs |  2.316 μs | 0.1270 μs |  7.26 |    0.03 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  83.348 μs |  7.679 μs | 0.4209 μs |  6.60 |    0.04 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 130.173 μs | 27.481 μs | 1.5064 μs | 10.30 |    0.11 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.042 μs |  1.631 μs | 0.0894 μs |  0.32 |    0.01 |  0.0229 |     400 B |        0.03 |
