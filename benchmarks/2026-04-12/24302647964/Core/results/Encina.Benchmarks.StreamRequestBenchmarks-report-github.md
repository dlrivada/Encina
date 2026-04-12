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
| Stream_SmallDataset_10Items             |   5.847 μs | 0.0252 μs | 0.0346 μs |  1.00 |    0.01 |  0.2518 |    4224 B |        1.00 |
| Stream_MediumDataset_100Items           |  34.602 μs | 0.1541 μs | 0.2160 μs |  5.92 |    0.05 |  1.2817 |   22224 B |        5.26 |
| Stream_LargeDataset_1000Items           | 316.285 μs | 0.7080 μs | 1.0377 μs | 54.10 |    0.36 | 11.7188 |  202227 B |       47.88 |
| Stream_WithPipelineBehaviors            |  38.408 μs | 0.2323 μs | 0.3256 μs |  6.57 |    0.07 |  1.0376 |   17952 B |        4.25 |
| Stream_MaterializeToList_100Items       |  37.505 μs | 0.1186 μs | 0.1662 μs |  6.41 |    0.05 |  1.8921 |   32040 B |        7.59 |
| Stream_CountOnly_NoMaterialization      |  34.613 μs | 0.0913 μs | 0.1281 μs |  5.92 |    0.04 |  1.2817 |   22224 B |        5.26 |
| Stream_WithCancellation_EarlyExit       |  69.953 μs | 0.6371 μs | 0.8721 μs | 11.96 |    0.16 |  1.8311 |   32600 B |        7.72 |
| Stream_DirectHandlerInvocation_NoEncina |   4.033 μs | 0.0223 μs | 0.0327 μs |  0.69 |    0.01 |  0.0229 |     400 B |        0.09 |
