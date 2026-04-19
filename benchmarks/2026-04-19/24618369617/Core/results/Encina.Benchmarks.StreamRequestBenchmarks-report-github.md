```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   5.838 μs | 0.0256 μs | 0.0375 μs |  1.00 |    0.01 |  0.2518 |    4224 B |        1.00 |
| Stream_MediumDataset_100Items           |  35.315 μs | 0.2104 μs | 0.3018 μs |  6.05 |    0.06 |  1.2817 |   22224 B |        5.26 |
| Stream_LargeDataset_1000Items           | 322.510 μs | 2.9348 μs | 4.1142 μs | 55.25 |    0.78 | 11.7188 |  202227 B |       47.88 |
| Stream_WithPipelineBehaviors            |  37.323 μs | 0.1002 μs | 0.1499 μs |  6.39 |    0.05 |  1.0376 |   17952 B |        4.25 |
| Stream_MaterializeToList_100Items       |  38.249 μs | 0.1767 μs | 0.2590 μs |  6.55 |    0.06 |  1.8921 |   32040 B |        7.59 |
| Stream_CountOnly_NoMaterialization      |  35.065 μs | 0.2042 μs | 0.3056 μs |  6.01 |    0.06 |  1.2817 |   22224 B |        5.26 |
| Stream_WithCancellation_EarlyExit       |  70.714 μs | 0.2311 μs | 0.3314 μs | 12.11 |    0.09 |  1.8311 |   32600 B |        7.72 |
| Stream_DirectHandlerInvocation_NoEncina |   4.149 μs | 0.0245 μs | 0.0351 μs |  0.71 |    0.01 |  0.0229 |     400 B |        0.09 |
