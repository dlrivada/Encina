```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   5.813 μs | 0.0238 μs | 0.0326 μs |  1.00 |    0.01 |  0.2518 |    4224 B |        1.00 |
| Stream_MediumDataset_100Items           |  34.749 μs | 0.2056 μs | 0.3014 μs |  5.98 |    0.06 |  1.2817 |   22224 B |        5.26 |
| Stream_LargeDataset_1000Items           | 319.537 μs | 1.2694 μs | 1.8205 μs | 54.97 |    0.43 | 11.7188 |  202227 B |       47.88 |
| Stream_WithPipelineBehaviors            |  38.552 μs | 0.0897 μs | 0.1315 μs |  6.63 |    0.04 |  1.0376 |   17952 B |        4.25 |
| Stream_MaterializeToList_100Items       |  38.020 μs | 0.1898 μs | 0.2660 μs |  6.54 |    0.06 |  1.8921 |   32040 B |        7.59 |
| Stream_CountOnly_NoMaterialization      |  34.524 μs | 0.1672 μs | 0.2451 μs |  5.94 |    0.05 |  1.2817 |   22224 B |        5.26 |
| Stream_WithCancellation_EarlyExit       |  69.919 μs | 0.2158 μs | 0.3230 μs | 12.03 |    0.09 |  1.8311 |   32600 B |        7.72 |
| Stream_DirectHandlerInvocation_NoEncina |   4.060 μs | 0.0259 μs | 0.0363 μs |  0.70 |    0.01 |  0.0229 |     400 B |        0.09 |
