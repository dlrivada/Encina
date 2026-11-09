```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.746 μs | 10.5798 μs | 0.5799 μs |  1.00 |    0.06 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  81.731 μs | 10.4601 μs | 0.5734 μs |  6.42 |    0.25 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 768.148 μs | 41.1140 μs | 2.2536 μs | 60.35 |    2.32 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  66.567 μs |  2.2067 μs | 0.1210 μs |  5.23 |    0.20 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  86.720 μs |  9.6816 μs | 0.5307 μs |  6.81 |    0.26 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  81.942 μs |  7.3044 μs | 0.4004 μs |  6.44 |    0.25 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 124.263 μs |  0.9335 μs | 0.0512 μs |  9.76 |    0.38 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.040 μs |  0.2247 μs | 0.0123 μs |  0.32 |    0.01 |  0.0229 |     400 B |        0.03 |
