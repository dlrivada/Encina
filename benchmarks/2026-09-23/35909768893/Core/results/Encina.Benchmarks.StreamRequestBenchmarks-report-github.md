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
| Stream_SmallDataset_10Items             |   8.543 μs |  0.0409 μs | 0.0022 μs |  1.00 |    0.00 | 0.1068 |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  60.986 μs |  1.3905 μs | 0.0762 μs |  7.14 |    0.01 | 0.6104 |   58848 B |        6.42 |
| Stream_LargeDataset_1000Items           | 578.816 μs | 11.2040 μs | 0.6141 μs | 67.75 |    0.06 | 5.8594 |  555650 B |       60.61 |
| Stream_WithPipelineBehaviors            |  48.095 μs | 13.9794 μs | 0.7663 μs |  5.63 |    0.08 | 0.4272 |   36976 B |        4.03 |
| Stream_MaterializeToList_100Items       |  63.489 μs |  2.4157 μs | 0.1324 μs |  7.43 |    0.01 | 0.7324 |   68664 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  60.386 μs |  2.0010 μs | 0.1097 μs |  7.07 |    0.01 | 0.6714 |   58848 B |        6.42 |
| Stream_WithCancellation_EarlyExit       |  95.146 μs |  5.3505 μs | 0.2933 μs | 11.14 |    0.03 | 0.8545 |   74624 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.449 μs |  0.1222 μs | 0.0067 μs |  0.52 |    0.00 |      - |     400 B |        0.04 |
