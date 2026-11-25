```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.293 μs |  1.474 μs | 0.0808 μs |  1.00 |    0.01 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  89.970 μs |  2.160 μs | 0.1184 μs |  7.32 |    0.04 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 780.340 μs | 77.120 μs | 4.2272 μs | 63.48 |    0.47 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  67.710 μs |  6.310 μs | 0.3459 μs |  5.51 |    0.04 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  87.541 μs |  5.212 μs | 0.2857 μs |  7.12 |    0.05 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  84.393 μs |  1.832 μs | 0.1004 μs |  6.87 |    0.04 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 127.998 μs |  7.330 μs | 0.4018 μs | 10.41 |    0.07 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.044 μs |  1.151 μs | 0.0631 μs |  0.33 |    0.00 |  0.0229 |     400 B |        0.03 |
