```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  10.775 μs |  0.1101 μs | 0.0060 μs |  1.00 |    0.00 | 0.1526 |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  76.320 μs |  0.6409 μs | 0.0351 μs |  7.08 |    0.00 | 0.9766 |   87016 B |        6.77 |
| Stream_LargeDataset_1000Items           | 737.232 μs | 21.8997 μs | 1.2004 μs | 68.42 |    0.10 | 9.7656 |  828618 B |       64.45 |
| Stream_WithPipelineBehaviors            |  56.138 μs |  0.7481 μs | 0.0410 μs |  5.21 |    0.00 | 0.6104 |   51544 B |        4.01 |
| Stream_MaterializeToList_100Items       |  79.097 μs |  1.2888 μs | 0.0706 μs |  7.34 |    0.01 | 1.0986 |   96832 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  76.741 μs |  1.4292 μs | 0.0783 μs |  7.12 |    0.01 | 0.9766 |   87016 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 110.676 μs |  3.4096 μs | 0.1869 μs | 10.27 |    0.02 | 1.2207 |  102792 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   4.548 μs |  0.3567 μs | 0.0196 μs |  0.42 |    0.00 |      - |     400 B |        0.03 |
