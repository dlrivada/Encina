```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.35GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   6.237 μs |  1.2891 μs | 0.0707 μs |  1.00 |    0.01 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  41.826 μs |  8.6297 μs | 0.4730 μs |  6.71 |    0.09 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 383.613 μs | 48.6714 μs | 2.6678 μs | 61.51 |    0.70 | 49.3164 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  35.858 μs |  6.4387 μs | 0.3529 μs |  5.75 |    0.07 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  43.394 μs |  6.1664 μs | 0.3380 μs |  6.96 |    0.08 |  5.7373 | 0.0610 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  42.739 μs | 11.8085 μs | 0.6473 μs |  6.85 |    0.11 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  63.661 μs | 38.9103 μs | 2.1328 μs | 10.21 |    0.31 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   2.287 μs |  0.5032 μs | 0.0276 μs |  0.37 |    0.01 |  0.0229 |      - |     400 B |        0.03 |
