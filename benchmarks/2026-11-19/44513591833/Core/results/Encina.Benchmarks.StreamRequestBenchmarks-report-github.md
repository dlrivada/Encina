```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  11.219 μs |  0.6233 μs | 0.0342 μs |  1.00 |    0.00 | 0.1526 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  79.207 μs |  1.2124 μs | 0.0665 μs |  7.06 |    0.02 | 0.9766 |   87024 B |        6.76 |
| Stream_LargeDataset_1000Items           | 761.130 μs | 57.1129 μs | 3.1306 μs | 67.84 |    0.30 | 9.7656 |  828626 B |       64.41 |
| Stream_WithPipelineBehaviors            |  57.072 μs |  0.7754 μs | 0.0425 μs |  5.09 |    0.01 | 0.6104 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  85.147 μs | 88.0074 μs | 4.8240 μs |  7.59 |    0.37 | 1.0986 |   96840 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  79.463 μs |  2.2044 μs | 0.1208 μs |  7.08 |    0.02 | 0.9766 |   87024 B |        6.76 |
| Stream_WithCancellation_EarlyExit       | 115.432 μs |  3.2959 μs | 0.1807 μs | 10.29 |    0.03 | 1.2207 |  102800 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.526 μs |  0.1963 μs | 0.0108 μs |  0.40 |    0.00 |      - |     400 B |        0.03 |
