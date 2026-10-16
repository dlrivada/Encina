```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.598 μs |  0.9283 μs | 0.0509 μs |  1.00 |    0.00 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  83.866 μs | 11.5460 μs | 0.6329 μs |  6.66 |    0.05 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 788.596 μs | 46.8189 μs | 2.5663 μs | 62.60 |    0.28 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  69.508 μs |  0.5609 μs | 0.0307 μs |  5.52 |    0.02 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  87.517 μs |  9.0258 μs | 0.4947 μs |  6.95 |    0.04 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  83.930 μs |  7.9065 μs | 0.4334 μs |  6.66 |    0.04 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 130.456 μs | 20.8952 μs | 1.1453 μs | 10.36 |    0.09 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.373 μs |  0.3476 μs | 0.0191 μs |  0.35 |    0.00 |  0.0229 |     400 B |        0.03 |
