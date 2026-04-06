```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                  | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|-----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   5.975 μs | 0.0128 μs | 0.0184 μs |   5.972 μs |  1.00 |    0.00 |  0.2518 |    4224 B |        1.00 |
| Stream_MediumDataset_100Items           |  35.702 μs | 0.1425 μs | 0.2043 μs |  35.795 μs |  5.98 |    0.04 |  1.2817 |   22224 B |        5.26 |
| Stream_LargeDataset_1000Items           | 334.113 μs | 1.1302 μs | 1.6209 μs | 333.259 μs | 55.92 |    0.32 | 11.7188 |  202227 B |       47.88 |
| Stream_WithPipelineBehaviors            |  38.757 μs | 0.1879 μs | 0.2754 μs |  38.655 μs |  6.49 |    0.05 |  1.0376 |   17952 B |        4.25 |
| Stream_MaterializeToList_100Items       |  38.809 μs | 0.1827 μs | 0.2561 μs |  38.948 μs |  6.50 |    0.05 |  1.8921 |   32040 B |        7.59 |
| Stream_CountOnly_NoMaterialization      |  35.918 μs | 0.3848 μs | 0.5267 μs |  36.247 μs |  6.01 |    0.09 |  1.2817 |   22224 B |        5.26 |
| Stream_WithCancellation_EarlyExit       |  69.969 μs | 0.2476 μs | 0.3705 μs |  70.014 μs | 11.71 |    0.07 |  1.8311 |   32600 B |        7.72 |
| Stream_DirectHandlerInvocation_NoEncina |   4.059 μs | 0.0163 μs | 0.0238 μs |   4.050 μs |  0.68 |    0.00 |  0.0229 |     400 B |        0.09 |
