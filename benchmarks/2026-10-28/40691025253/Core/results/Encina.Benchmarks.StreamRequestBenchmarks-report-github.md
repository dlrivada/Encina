```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.107 μs |  1.3091 μs | 0.0718 μs |  1.00 |    0.01 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  80.363 μs |  7.8226 μs | 0.4288 μs |  6.64 |    0.05 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 772.572 μs | 89.5291 μs | 4.9074 μs | 63.81 |    0.48 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  68.040 μs |  5.3920 μs | 0.2956 μs |  5.62 |    0.04 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  87.152 μs |  8.5436 μs | 0.4683 μs |  7.20 |    0.05 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  84.197 μs |  8.5322 μs | 0.4677 μs |  6.95 |    0.05 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 127.944 μs |  2.7447 μs | 0.1504 μs | 10.57 |    0.06 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.017 μs |  0.9959 μs | 0.0546 μs |  0.33 |    0.00 |  0.0229 |     400 B |        0.03 |
