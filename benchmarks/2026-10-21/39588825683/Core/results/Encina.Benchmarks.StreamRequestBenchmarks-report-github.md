```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.729 μs |  0.6073 μs | 0.0333 μs |  1.00 |    0.00 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  84.530 μs |  0.1831 μs | 0.0100 μs |  6.64 |    0.02 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 813.239 μs | 10.9354 μs | 0.5994 μs | 63.89 |    0.15 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  70.218 μs |  1.1291 μs | 0.0619 μs |  5.52 |    0.01 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  95.992 μs |  8.3443 μs | 0.4574 μs |  7.54 |    0.04 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  87.545 μs |  8.1602 μs | 0.4473 μs |  6.88 |    0.03 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 132.272 μs | 32.3286 μs | 1.7720 μs | 10.39 |    0.12 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.064 μs |  1.4406 μs | 0.0790 μs |  0.32 |    0.01 |  0.0229 |     400 B |        0.03 |
