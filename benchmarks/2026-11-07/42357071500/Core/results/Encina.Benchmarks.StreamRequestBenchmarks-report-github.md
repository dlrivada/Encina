```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.341 μs |  0.4304 μs | 0.0236 μs |  1.00 |    0.00 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  63.549 μs | 14.8650 μs | 0.8148 μs |  6.80 |    0.08 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 611.113 μs | 17.1803 μs | 0.9417 μs | 65.42 |    0.17 | 48.8281 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  51.025 μs |  5.6870 μs | 0.3117 μs |  5.46 |    0.03 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  67.700 μs |  7.1455 μs | 0.3917 μs |  7.25 |    0.04 |  5.7373 |      - |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  65.518 μs |  2.9715 μs | 0.1629 μs |  7.01 |    0.02 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  95.728 μs | 15.5664 μs | 0.8532 μs | 10.25 |    0.08 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.425 μs |  0.9572 μs | 0.0525 μs |  0.37 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
