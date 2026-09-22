```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   6.077 μs | 0.1088 μs | 0.0060 μs |  1.00 |    0.00 |  0.2518 |    4224 B |        1.00 |
| Stream_MediumDataset_100Items           |  36.210 μs | 2.8526 μs | 0.1564 μs |  5.96 |    0.02 |  1.2817 |   22224 B |        5.26 |
| Stream_LargeDataset_1000Items           | 333.036 μs | 5.7338 μs | 0.3143 μs | 54.81 |    0.06 | 11.7188 |  202227 B |       47.88 |
| Stream_WithPipelineBehaviors            |  38.805 μs | 1.3201 μs | 0.0724 μs |  6.39 |    0.01 |  1.0376 |   17952 B |        4.25 |
| Stream_MaterializeToList_100Items       |  38.830 μs | 0.5025 μs | 0.0275 μs |  6.39 |    0.01 |  1.8921 |   32040 B |        7.59 |
| Stream_CountOnly_NoMaterialization      |  35.948 μs | 1.2630 μs | 0.0692 μs |  5.92 |    0.01 |  1.2817 |   22224 B |        5.26 |
| Stream_WithCancellation_EarlyExit       |  73.940 μs | 2.1387 μs | 0.1172 μs | 12.17 |    0.02 |  1.8311 |   32600 B |        7.72 |
| Stream_DirectHandlerInvocation_NoEncina |   4.091 μs | 0.3521 μs | 0.0193 μs |  0.67 |    0.00 |  0.0229 |     400 B |        0.09 |
