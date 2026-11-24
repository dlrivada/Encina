```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   8.450 μs |  0.5188 μs | 0.0284 μs |  1.00 |    0.00 | 0.1526 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  59.634 μs |  1.8502 μs | 0.1014 μs |  7.06 |    0.02 | 1.0376 |   87024 B |        6.76 |
| Stream_LargeDataset_1000Items           | 570.684 μs | 29.0224 μs | 1.5908 μs | 67.54 |    0.26 | 9.7656 |  828626 B |       64.41 |
| Stream_WithPipelineBehaviors            |  42.754 μs |  0.9955 μs | 0.0546 μs |  5.06 |    0.02 | 0.6104 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  62.029 μs |  7.7562 μs | 0.4251 μs |  7.34 |    0.05 | 1.0986 |   96840 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  59.734 μs |  1.7143 μs | 0.0940 μs |  7.07 |    0.02 | 1.0376 |   87024 B |        6.76 |
| Stream_WithCancellation_EarlyExit       |  86.366 μs |  2.9402 μs | 0.1612 μs | 10.22 |    0.03 | 1.2207 |  102800 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.119 μs |  0.1464 μs | 0.0080 μs |  0.37 |    0.00 | 0.0038 |     400 B |        0.03 |
