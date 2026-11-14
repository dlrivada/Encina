```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.84GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.349 μs |  3.2609 μs | 0.1787 μs |  1.00 |    0.02 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  81.812 μs |  9.6804 μs | 0.5306 μs |  6.63 |    0.09 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 762.704 μs | 46.9734 μs | 2.5748 μs | 61.77 |    0.79 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  68.513 μs |  8.4922 μs | 0.4655 μs |  5.55 |    0.08 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  86.999 μs |  9.3954 μs | 0.5150 μs |  7.05 |    0.09 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  82.163 μs | 26.5564 μs | 1.4556 μs |  6.65 |    0.13 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 124.929 μs | 13.9973 μs | 0.7672 μs | 10.12 |    0.14 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.169 μs |  0.5066 μs | 0.0278 μs |  0.34 |    0.00 |  0.0229 |     400 B |        0.03 |
