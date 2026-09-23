```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   5.967 μs |  0.2962 μs | 0.0162 μs |  1.00 |    0.00 |  0.2518 |    4224 B |        1.00 |
| Stream_MediumDataset_100Items           |  35.267 μs |  2.5606 μs | 0.1404 μs |  5.91 |    0.02 |  1.2817 |   22224 B |        5.26 |
| Stream_LargeDataset_1000Items           | 325.144 μs | 58.1052 μs | 3.1849 μs | 54.49 |    0.48 | 11.7188 |  202227 B |       47.88 |
| Stream_WithPipelineBehaviors            |  38.493 μs |  1.4703 μs | 0.0806 μs |  6.45 |    0.02 |  1.0376 |   17952 B |        4.25 |
| Stream_MaterializeToList_100Items       |  38.803 μs |  2.5767 μs | 0.1412 μs |  6.50 |    0.03 |  1.8921 |   32040 B |        7.59 |
| Stream_CountOnly_NoMaterialization      |  35.613 μs |  1.6551 μs | 0.0907 μs |  5.97 |    0.02 |  1.2817 |   22224 B |        5.26 |
| Stream_WithCancellation_EarlyExit       |  70.151 μs |  7.9668 μs | 0.4367 μs | 11.76 |    0.07 |  1.8311 |   32600 B |        7.72 |
| Stream_DirectHandlerInvocation_NoEncina |   4.091 μs |  0.3161 μs | 0.0173 μs |  0.69 |    0.00 |  0.0229 |     400 B |        0.09 |
