```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   6.340 μs |  0.8721 μs | 0.0478 μs |  1.00 |    0.01 |  0.2518 |    4224 B |        1.00 |
| Stream_MediumDataset_100Items           |  35.377 μs |  3.2755 μs | 0.1795 μs |  5.58 |    0.04 |  1.2817 |   22224 B |        5.26 |
| Stream_LargeDataset_1000Items           | 317.419 μs | 28.9787 μs | 1.5884 μs | 50.07 |    0.39 | 11.7188 |  202227 B |       47.88 |
| Stream_WithPipelineBehaviors            |  38.999 μs |  1.2600 μs | 0.0691 μs |  6.15 |    0.04 |  1.0376 |   17952 B |        4.25 |
| Stream_MaterializeToList_100Items       |  37.742 μs |  3.0933 μs | 0.1696 μs |  5.95 |    0.05 |  1.8921 |   32040 B |        7.59 |
| Stream_CountOnly_NoMaterialization      |  34.804 μs |  0.5557 μs | 0.0305 μs |  5.49 |    0.04 |  1.2817 |   22224 B |        5.26 |
| Stream_WithCancellation_EarlyExit       |  68.724 μs |  0.8277 μs | 0.0454 μs | 10.84 |    0.07 |  1.8311 |   32600 B |        7.72 |
| Stream_DirectHandlerInvocation_NoEncina |   4.348 μs |  0.9717 μs | 0.0533 μs |  0.69 |    0.01 |  0.0229 |     400 B |        0.09 |
