```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.755 μs |   3.4969 μs | 0.1917 μs |  1.00 |    0.02 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  82.808 μs |   6.6162 μs | 0.3627 μs |  6.49 |    0.09 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 795.892 μs | 123.1418 μs | 6.7498 μs | 62.41 |    0.94 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  69.779 μs |   2.2404 μs | 0.1228 μs |  5.47 |    0.07 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  86.011 μs |   2.4171 μs | 0.1325 μs |  6.74 |    0.09 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  85.245 μs |  12.3056 μs | 0.6745 μs |  6.68 |    0.10 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 128.419 μs |  11.9436 μs | 0.6547 μs | 10.07 |    0.14 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.384 μs |   0.6521 μs | 0.0357 μs |  0.34 |    0.01 |  0.0229 |     400 B |        0.03 |
