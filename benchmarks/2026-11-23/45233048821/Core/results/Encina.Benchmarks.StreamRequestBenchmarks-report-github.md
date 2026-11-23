```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.872 μs |  3.890 μs | 0.2132 μs |  1.00 |    0.03 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  64.559 μs |  5.301 μs | 0.2906 μs |  6.54 |    0.12 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 605.848 μs | 54.585 μs | 2.9920 μs | 61.39 |    1.17 | 48.8281 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  50.906 μs |  2.327 μs | 0.1276 μs |  5.16 |    0.10 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  67.089 μs | 10.525 μs | 0.5769 μs |  6.80 |    0.14 |  5.7373 |      - |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  63.753 μs |  4.073 μs | 0.2232 μs |  6.46 |    0.12 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  95.116 μs |  5.673 μs | 0.3109 μs |  9.64 |    0.18 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.439 μs |  1.362 μs | 0.0746 μs |  0.35 |    0.01 |  0.0229 |      - |     400 B |        0.03 |
