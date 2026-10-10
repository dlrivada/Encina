```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.284 μs |  1.175 μs | 0.0644 μs |  1.00 |    0.01 |  0.7629 |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  82.591 μs | 12.347 μs | 0.6768 μs |  6.72 |    0.06 |  5.1270 |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 787.683 μs | 88.957 μs | 4.8760 μs | 64.13 |    0.45 | 48.8281 |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  68.270 μs |  2.516 μs | 0.1379 μs |  5.56 |    0.03 |  3.0518 |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  92.052 μs | 11.299 μs | 0.6194 μs |  7.49 |    0.06 |  5.7373 |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  81.566 μs |  9.707 μs | 0.5321 μs |  6.64 |    0.05 |  5.1270 |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 130.127 μs |  7.861 μs | 0.4309 μs | 10.59 |    0.06 |  6.1035 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   4.110 μs |  1.068 μs | 0.0585 μs |  0.33 |    0.00 |  0.0229 |     400 B |        0.03 |
