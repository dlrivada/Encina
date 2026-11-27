```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  11.860 μs |  1.6805 μs | 0.0921 μs |  1.00 |    0.01 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  79.035 μs | 16.7722 μs | 0.9193 μs |  6.66 |    0.08 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 738.477 μs | 60.8525 μs | 3.3355 μs | 62.27 |    0.49 | 48.8281 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  66.284 μs | 11.7975 μs | 0.6467 μs |  5.59 |    0.06 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  85.052 μs |  4.5179 μs | 0.2476 μs |  7.17 |    0.05 |  5.7373 |      - |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  79.086 μs | 16.3221 μs | 0.8947 μs |  6.67 |    0.08 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 121.651 μs | 10.2444 μs | 0.5615 μs | 10.26 |    0.08 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.967 μs |  0.3196 μs | 0.0175 μs |  0.33 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
