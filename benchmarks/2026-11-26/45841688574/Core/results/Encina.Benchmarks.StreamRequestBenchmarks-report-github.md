```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.08GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.346 μs |  0.7283 μs | 0.0399 μs |  1.00 |    0.00 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  82.404 μs | 22.6677 μs | 1.2425 μs |  6.67 |    0.09 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 806.708 μs | 34.5226 μs | 1.8923 μs | 65.34 |    0.23 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  66.780 μs |  5.6580 μs | 0.3101 μs |  5.41 |    0.03 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  84.472 μs |  7.8466 μs | 0.4301 μs |  6.84 |    0.04 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  80.704 μs |  9.5394 μs | 0.5229 μs |  6.54 |    0.04 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 124.576 μs | 10.3425 μs | 0.5669 μs | 10.09 |    0.05 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.054 μs |  0.6429 μs | 0.0352 μs |  0.33 |    0.00 |  0.0229 |     400 B |        0.03 |
