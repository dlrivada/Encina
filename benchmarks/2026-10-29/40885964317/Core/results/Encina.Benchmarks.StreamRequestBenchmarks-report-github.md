```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.78GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.726 μs |  0.4818 μs | 0.0264 μs |  1.00 |    0.00 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  84.897 μs |  4.0711 μs | 0.2232 μs |  6.67 |    0.02 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 791.335 μs | 69.6111 μs | 3.8156 μs | 62.18 |    0.28 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  70.448 μs |  8.9991 μs | 0.4933 μs |  5.54 |    0.04 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  89.622 μs | 10.1229 μs | 0.5549 μs |  7.04 |    0.04 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  85.952 μs |  2.4560 μs | 0.1346 μs |  6.75 |    0.02 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 133.603 μs | 42.5891 μs | 2.3345 μs | 10.50 |    0.16 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.085 μs |  0.5925 μs | 0.0325 μs |  0.32 |    0.00 |  0.0229 |     400 B |        0.03 |
