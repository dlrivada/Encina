```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.83GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.070 μs |  1.0121 μs | 0.0555 μs |  1.00 |    0.01 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  79.872 μs |  2.1391 μs | 0.1173 μs |  6.62 |    0.03 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 762.318 μs | 28.2304 μs | 1.5474 μs | 63.16 |    0.28 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  66.479 μs |  2.4459 μs | 0.1341 μs |  5.51 |    0.02 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  86.588 μs | 29.0270 μs | 1.5911 μs |  7.17 |    0.12 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  80.002 μs |  8.6629 μs | 0.4748 μs |  6.63 |    0.04 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 127.539 μs | 12.7147 μs | 0.6969 μs | 10.57 |    0.07 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.997 μs |  0.2017 μs | 0.0111 μs |  0.33 |    0.00 |  0.0229 |     400 B |        0.03 |
