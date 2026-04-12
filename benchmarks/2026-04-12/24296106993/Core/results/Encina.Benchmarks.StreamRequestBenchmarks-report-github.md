```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   5.890 μs | 0.0197 μs | 0.0289 μs |  1.00 |    0.01 | 0.1678 |    4224 B |        1.00 |
| Stream_MediumDataset_100Items           |  37.940 μs | 0.1614 μs | 0.2366 μs |  6.44 |    0.05 | 0.8545 |   22224 B |        5.26 |
| Stream_LargeDataset_1000Items           | 355.636 μs | 0.6444 μs | 0.9446 μs | 60.39 |    0.33 | 7.8125 |  202226 B |       47.88 |
| Stream_WithPipelineBehaviors            |  42.915 μs | 0.2067 μs | 0.3094 μs |  7.29 |    0.06 | 0.6714 |   17952 B |        4.25 |
| Stream_MaterializeToList_100Items       |  41.100 μs | 0.1361 μs | 0.1908 μs |  6.98 |    0.05 | 1.2207 |   32040 B |        7.59 |
| Stream_CountOnly_NoMaterialization      |  37.730 μs | 0.0777 μs | 0.1064 μs |  6.41 |    0.04 | 0.8545 |   22224 B |        5.26 |
| Stream_WithCancellation_EarlyExit       |  71.340 μs | 0.2936 μs | 0.4116 μs | 12.11 |    0.09 | 1.2207 |   32600 B |        7.72 |
| Stream_DirectHandlerInvocation_NoEncina |   4.056 μs | 0.0057 μs | 0.0080 μs |  0.69 |    0.00 | 0.0153 |     400 B |        0.09 |
