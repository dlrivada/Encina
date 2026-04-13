```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                  | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   5.985 μs | 0.0536 μs | 0.0786 μs |   5.984 μs |  1.00 |    0.02 | 0.1678 |    4224 B |        1.00 |
| Stream_MediumDataset_100Items           |  38.217 μs | 0.1317 μs | 0.1889 μs |  38.237 μs |  6.39 |    0.09 | 0.8545 |   22224 B |        5.26 |
| Stream_LargeDataset_1000Items           | 360.943 μs | 1.0885 μs | 1.5259 μs | 360.649 μs | 60.32 |    0.82 | 7.8125 |  202226 B |       47.88 |
| Stream_WithPipelineBehaviors            |  43.766 μs | 0.5594 μs | 0.8023 μs |  43.321 μs |  7.31 |    0.16 | 0.6714 |   17952 B |        4.25 |
| Stream_MaterializeToList_100Items       |  42.108 μs | 0.2645 μs | 0.3876 μs |  42.134 μs |  7.04 |    0.11 | 1.2207 |   32040 B |        7.59 |
| Stream_CountOnly_NoMaterialization      |  38.465 μs | 0.0811 μs | 0.1163 μs |  38.449 μs |  6.43 |    0.09 | 0.8545 |   22224 B |        5.26 |
| Stream_WithCancellation_EarlyExit       |  72.187 μs | 0.2177 μs | 0.3052 μs |  72.190 μs | 12.06 |    0.16 | 1.2207 |   32600 B |        7.72 |
| Stream_DirectHandlerInvocation_NoEncina |   4.139 μs | 0.0465 μs | 0.0637 μs |   4.191 μs |  0.69 |    0.01 | 0.0153 |     400 B |        0.09 |
