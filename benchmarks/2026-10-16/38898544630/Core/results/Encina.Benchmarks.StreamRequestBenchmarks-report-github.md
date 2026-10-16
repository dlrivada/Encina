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
| Stream_SmallDataset_10Items             |  11.293 μs |  0.5661 μs | 0.0310 μs |  1.00 |    0.00 | 0.1526 |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  82.010 μs | 35.0145 μs | 1.9193 μs |  7.26 |    0.15 | 0.9766 |   87016 B |        6.77 |
| Stream_LargeDataset_1000Items           | 777.251 μs |  9.1209 μs | 0.4999 μs | 68.82 |    0.17 | 9.7656 |  828618 B |       64.45 |
| Stream_WithPipelineBehaviors            |  58.118 μs |  0.7133 μs | 0.0391 μs |  5.15 |    0.01 | 0.6104 |   51544 B |        4.01 |
| Stream_MaterializeToList_100Items       |  82.954 μs |  0.6487 μs | 0.0356 μs |  7.35 |    0.02 | 1.0986 |   96832 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  80.162 μs |  2.0856 μs | 0.1143 μs |  7.10 |    0.02 | 0.9766 |   87016 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 116.786 μs |  1.1160 μs | 0.0612 μs | 10.34 |    0.03 | 1.2207 |  102792 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   4.732 μs |  0.2562 μs | 0.0140 μs |  0.42 |    0.00 |      - |     400 B |        0.03 |
