```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.56GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.375 μs |  0.7655 μs | 0.0420 μs |  1.00 |    0.00 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  82.918 μs | 10.4366 μs | 0.5721 μs |  6.70 |    0.04 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 775.219 μs | 20.4825 μs | 1.1227 μs | 62.65 |    0.20 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  69.176 μs |  4.4532 μs | 0.2441 μs |  5.59 |    0.02 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  86.457 μs |  4.3830 μs | 0.2402 μs |  6.99 |    0.03 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  82.288 μs |  2.9120 μs | 0.1596 μs |  6.65 |    0.02 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 128.614 μs |  3.7929 μs | 0.2079 μs | 10.39 |    0.03 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.046 μs |  0.8850 μs | 0.0485 μs |  0.33 |    0.00 |  0.0229 |     400 B |        0.03 |
