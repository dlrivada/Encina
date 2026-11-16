```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.13GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.273 μs |  0.3422 μs | 0.0188 μs |  1.00 |    0.00 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  81.669 μs | 19.2565 μs | 1.0555 μs |  6.65 |    0.08 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 768.326 μs | 38.8405 μs | 2.1290 μs | 62.61 |    0.17 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  67.162 μs |  7.7821 μs | 0.4266 μs |  5.47 |    0.03 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  88.266 μs | 63.6093 μs | 3.4866 μs |  7.19 |    0.25 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  81.500 μs |  9.3189 μs | 0.5108 μs |  6.64 |    0.04 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 124.464 μs | 54.5246 μs | 2.9887 μs | 10.14 |    0.21 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.024 μs |  0.7882 μs | 0.0432 μs |  0.33 |    0.00 |  0.0229 |     400 B |        0.03 |
