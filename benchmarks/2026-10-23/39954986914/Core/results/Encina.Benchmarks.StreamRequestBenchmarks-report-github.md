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
| Stream_SmallDataset_10Items             |  11.153 μs |  0.4627 μs | 0.0254 μs |  1.00 |    0.00 | 0.1526 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  79.913 μs |  9.1746 μs | 0.5029 μs |  7.17 |    0.04 | 0.9766 |   87024 B |        6.76 |
| Stream_LargeDataset_1000Items           | 763.599 μs |  4.1131 μs | 0.2255 μs | 68.47 |    0.14 | 9.7656 |  828626 B |       64.41 |
| Stream_WithPipelineBehaviors            |  57.863 μs |  8.8020 μs | 0.4825 μs |  5.19 |    0.04 | 0.6104 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  83.012 μs | 14.6828 μs | 0.8048 μs |  7.44 |    0.06 | 1.0986 |   96840 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  78.021 μs |  4.3713 μs | 0.2396 μs |  7.00 |    0.02 | 0.9766 |   87024 B |        6.76 |
| Stream_WithCancellation_EarlyExit       | 116.756 μs |  4.0456 μs | 0.2218 μs | 10.47 |    0.03 | 1.2207 |  102800 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.580 μs |  0.3986 μs | 0.0218 μs |  0.41 |    0.00 |      - |     400 B |        0.03 |
