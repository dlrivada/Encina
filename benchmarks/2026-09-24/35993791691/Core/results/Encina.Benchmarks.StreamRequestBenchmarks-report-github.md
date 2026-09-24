```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.740 μs |   0.8380 μs | 0.0459 μs |  1.00 |    0.01 |  0.3510 |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  69.376 μs |   9.0173 μs | 0.4943 μs |  7.12 |    0.05 |  2.3193 |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 692.894 μs | 115.6747 μs | 6.3405 μs | 71.14 |    0.63 | 21.4844 |  555653 B |       60.61 |
| Stream_WithPipelineBehaviors            |  60.252 μs |   3.1529 μs | 0.1728 μs |  6.19 |    0.03 |  1.4648 |   36976 B |        4.03 |
| Stream_MaterializeToList_100Items       |  72.427 μs |   1.3361 μs | 0.0732 μs |  7.44 |    0.03 |  2.6855 |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  72.380 μs |   4.4811 μs | 0.2456 μs |  7.43 |    0.04 |  2.3193 |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       | 113.382 μs |  12.6306 μs | 0.6923 μs | 11.64 |    0.08 |  2.9297 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.137 μs |   0.1320 μs | 0.0072 μs |  0.42 |    0.00 |  0.0153 |     400 B |        0.04 |
