```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|-----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.466 μs |   0.3245 μs |  0.0178 μs |  1.00 |    0.00 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  81.860 μs |   2.9323 μs |  0.1607 μs |  6.57 |    0.01 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 767.488 μs | 201.0379 μs | 11.0196 μs | 61.57 |    0.77 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  67.333 μs |   4.1325 μs |  0.2265 μs |  5.40 |    0.02 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  87.253 μs |   7.9535 μs |  0.4360 μs |  7.00 |    0.03 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  83.463 μs |   5.5565 μs |  0.3046 μs |  6.70 |    0.02 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 127.081 μs |  10.9145 μs |  0.5983 μs | 10.19 |    0.04 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.008 μs |   0.2767 μs |  0.0152 μs |  0.32 |    0.00 |  0.0229 |     400 B |        0.03 |
