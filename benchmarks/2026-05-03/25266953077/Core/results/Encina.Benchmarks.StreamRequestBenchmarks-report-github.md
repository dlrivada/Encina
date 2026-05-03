```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   5.918 μs | 0.0122 μs | 0.0178 μs |  1.00 |    0.00 | 0.1678 |    4224 B |        1.00 |
| Stream_MediumDataset_100Items           |  37.643 μs | 0.0920 μs | 0.1349 μs |  6.36 |    0.03 | 0.8545 |   22224 B |        5.26 |
| Stream_LargeDataset_1000Items           | 354.334 μs | 0.7542 μs | 1.1055 μs | 59.88 |    0.26 | 7.8125 |  202226 B |       47.88 |
| Stream_WithPipelineBehaviors            |  42.048 μs | 0.0551 μs | 0.0825 μs |  7.11 |    0.03 | 0.6714 |   17952 B |        4.25 |
| Stream_MaterializeToList_100Items       |  40.901 μs | 0.0927 μs | 0.1330 μs |  6.91 |    0.03 | 1.2207 |   32040 B |        7.59 |
| Stream_CountOnly_NoMaterialization      |  37.592 μs | 0.0817 μs | 0.1172 μs |  6.35 |    0.03 | 0.8545 |   22224 B |        5.26 |
| Stream_WithCancellation_EarlyExit       |  71.652 μs | 0.5394 μs | 0.7736 μs | 12.11 |    0.13 | 1.2207 |   32600 B |        7.72 |
| Stream_DirectHandlerInvocation_NoEncina |   3.995 μs | 0.0040 μs | 0.0056 μs |  0.68 |    0.00 | 0.0153 |     400 B |        0.09 |
