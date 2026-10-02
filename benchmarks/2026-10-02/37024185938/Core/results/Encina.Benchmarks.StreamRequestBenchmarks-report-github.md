```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.574 μs |  0.4702 μs | 0.0258 μs |  1.00 |    0.00 |  0.3510 |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  70.992 μs | 11.5220 μs | 0.6316 μs |  7.42 |    0.06 |  2.3193 |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 679.442 μs | 59.3327 μs | 3.2522 μs | 70.97 |    0.34 | 21.4844 |  555653 B |       60.61 |
| Stream_WithPipelineBehaviors            |  60.242 μs |  3.2309 μs | 0.1771 μs |  6.29 |    0.02 |  1.4648 |   36976 B |        4.03 |
| Stream_MaterializeToList_100Items       |  71.940 μs | 16.3973 μs | 0.8988 μs |  7.51 |    0.08 |  2.6855 |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  70.513 μs | 12.7760 μs | 0.7003 μs |  7.37 |    0.07 |  2.3193 |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       | 110.944 μs | 13.1643 μs | 0.7216 μs | 11.59 |    0.07 |  2.9297 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.134 μs |  0.0200 μs | 0.0011 μs |  0.43 |    0.00 |  0.0153 |     400 B |        0.04 |
