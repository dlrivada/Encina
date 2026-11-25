```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.38GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   7.122 μs |   3.457 μs |  0.1895 μs |  1.00 |    0.03 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  46.440 μs |  17.433 μs |  0.9555 μs |  6.52 |    0.19 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 435.300 μs | 287.451 μs | 15.7562 μs | 61.15 |    2.37 | 49.3164 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  39.798 μs |  20.118 μs |  1.1027 μs |  5.59 |    0.18 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  46.374 μs |   8.795 μs |  0.4821 μs |  6.51 |    0.16 |  5.7373 | 0.0610 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  43.494 μs |   9.414 μs |  0.5160 μs |  6.11 |    0.15 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  65.122 μs |  30.535 μs |  1.6737 μs |  9.15 |    0.29 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   2.401 μs |   4.556 μs |  0.2497 μs |  0.34 |    0.03 |  0.0229 |      - |     400 B |        0.03 |
