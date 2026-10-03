```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   7.100 μs |   1.0544 μs | 0.0578 μs |  1.00 |    0.01 | 0.1068 |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  50.405 μs |   4.6209 μs | 0.2533 μs |  7.10 |    0.06 | 0.6714 |   58848 B |        6.42 |
| Stream_LargeDataset_1000Items           | 490.103 μs | 123.7463 μs | 6.7830 μs | 69.03 |    0.96 | 5.8594 |  555650 B |       60.61 |
| Stream_WithPipelineBehaviors            |  40.336 μs |   9.5529 μs | 0.5236 μs |  5.68 |    0.08 | 0.4272 |   36976 B |        4.03 |
| Stream_MaterializeToList_100Items       |  51.538 μs |  11.8197 μs | 0.6479 μs |  7.26 |    0.09 | 0.7935 |   68664 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  49.843 μs |  13.3651 μs | 0.7326 μs |  7.02 |    0.10 | 0.6714 |   58848 B |        6.42 |
| Stream_WithCancellation_EarlyExit       |  78.550 μs |  12.7038 μs | 0.6963 μs | 11.06 |    0.12 | 0.8545 |   74624 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   3.702 μs |   0.1629 μs | 0.0089 μs |  0.52 |    0.00 | 0.0038 |     400 B |        0.04 |
