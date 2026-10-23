```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  10.785 μs |  1.1487 μs | 0.0630 μs |  1.00 |    0.01 | 0.1526 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  76.888 μs |  3.8439 μs | 0.2107 μs |  7.13 |    0.04 | 0.9766 |   87024 B |        6.76 |
| Stream_LargeDataset_1000Items           | 741.505 μs | 19.4856 μs | 1.0681 μs | 68.75 |    0.36 | 9.7656 |  828626 B |       64.41 |
| Stream_WithPipelineBehaviors            |  55.582 μs |  0.0375 μs | 0.0021 μs |  5.15 |    0.03 | 0.6104 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  80.053 μs |  2.6521 μs | 0.1454 μs |  7.42 |    0.04 | 1.0986 |   96840 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  76.893 μs |  3.6675 μs | 0.2010 μs |  7.13 |    0.04 | 0.9766 |   87024 B |        6.76 |
| Stream_WithCancellation_EarlyExit       | 112.537 μs |  2.7238 μs | 0.1493 μs | 10.43 |    0.05 | 1.2207 |  102800 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.528 μs |  0.5866 μs | 0.0322 μs |  0.42 |    0.00 |      - |     400 B |        0.03 |
