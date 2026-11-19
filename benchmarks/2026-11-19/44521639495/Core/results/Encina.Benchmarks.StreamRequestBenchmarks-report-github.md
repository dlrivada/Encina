```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.855 μs |  3.3564 μs | 0.1840 μs |  1.00 |    0.02 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  65.566 μs |  6.5265 μs | 0.3577 μs |  6.65 |    0.11 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 599.326 μs | 48.0632 μs | 2.6345 μs | 60.83 |    1.00 | 48.8281 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  50.837 μs |  5.8620 μs | 0.3213 μs |  5.16 |    0.09 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  69.882 μs | 17.3312 μs | 0.9500 μs |  7.09 |    0.14 |  5.7373 |      - |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  65.072 μs |  5.6760 μs | 0.3111 μs |  6.60 |    0.11 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  96.935 μs | 22.7746 μs | 1.2484 μs |  9.84 |    0.19 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.457 μs |  0.3623 μs | 0.0199 μs |  0.35 |    0.01 |  0.0229 |      - |     400 B |        0.03 |
