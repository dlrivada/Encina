```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   6.321 μs |   0.6628 μs | 0.0363 μs |  1.00 |    0.01 | 0.1068 |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  44.617 μs |   1.5000 μs | 0.0822 μs |  7.06 |    0.04 | 0.6714 |   58848 B |        6.42 |
| Stream_LargeDataset_1000Items           | 429.718 μs | 180.7165 μs | 9.9057 μs | 67.98 |    1.40 | 6.3477 |  555650 B |       60.61 |
| Stream_WithPipelineBehaviors            |  34.812 μs |  22.6836 μs | 1.2434 μs |  5.51 |    0.17 | 0.4272 |   36976 B |        4.03 |
| Stream_MaterializeToList_100Items       |  48.917 μs |  13.4765 μs | 0.7387 μs |  7.74 |    0.11 | 0.7935 |   68664 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  44.794 μs |  17.5105 μs | 0.9598 μs |  7.09 |    0.14 | 0.6714 |   58848 B |        6.42 |
| Stream_WithCancellation_EarlyExit       |  69.239 μs |  32.8056 μs | 1.7982 μs | 10.95 |    0.25 | 0.8545 |   74624 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   2.852 μs |   0.0624 μs | 0.0034 μs |  0.45 |    0.00 | 0.0038 |     400 B |        0.04 |
