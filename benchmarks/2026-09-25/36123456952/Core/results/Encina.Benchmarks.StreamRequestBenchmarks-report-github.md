```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   6.498 μs |  2.423 μs | 0.1328 μs |  1.00 |    0.02 | 0.1068 |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  44.541 μs |  1.969 μs | 0.1079 μs |  6.86 |    0.12 | 0.6714 |   58848 B |        6.42 |
| Stream_LargeDataset_1000Items           | 426.350 μs | 15.580 μs | 0.8540 μs | 65.63 |    1.15 | 6.3477 |  555650 B |       60.61 |
| Stream_WithPipelineBehaviors            |  34.445 μs |  5.987 μs | 0.3282 μs |  5.30 |    0.10 | 0.4272 |   36976 B |        4.03 |
| Stream_MaterializeToList_100Items       |  49.556 μs |  2.958 μs | 0.1622 μs |  7.63 |    0.14 | 0.7935 |   68664 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  50.173 μs | 20.833 μs | 1.1419 μs |  7.72 |    0.20 | 0.6714 |   58848 B |        6.42 |
| Stream_WithCancellation_EarlyExit       |  77.113 μs | 42.713 μs | 2.3412 μs | 11.87 |    0.37 | 0.8545 |   74624 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   3.027 μs |  2.929 μs | 0.1605 μs |  0.47 |    0.02 | 0.0038 |     400 B |        0.04 |
