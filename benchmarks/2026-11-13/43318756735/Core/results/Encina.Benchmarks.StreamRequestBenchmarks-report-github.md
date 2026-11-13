```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   8.985 μs |  2.1330 μs | 0.1169 μs |  1.00 |    0.02 | 0.1526 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  71.008 μs | 43.4482 μs | 2.3815 μs |  7.90 |    0.25 | 0.9766 |   87024 B |        6.76 |
| Stream_LargeDataset_1000Items           | 634.313 μs | 10.8960 μs | 0.5972 μs | 70.60 |    0.79 | 9.7656 |  828626 B |       64.41 |
| Stream_WithPipelineBehaviors            |  45.566 μs |  3.5078 μs | 0.1923 μs |  5.07 |    0.06 | 0.6104 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  70.596 μs |  9.5843 μs | 0.5253 μs |  7.86 |    0.10 | 1.0986 |   96840 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  68.486 μs | 22.8126 μs | 1.2504 μs |  7.62 |    0.15 | 0.9766 |   87024 B |        6.76 |
| Stream_WithCancellation_EarlyExit       |  90.419 μs | 35.6403 μs | 1.9536 μs | 10.06 |    0.22 | 1.2207 |  102800 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.155 μs |  0.3466 μs | 0.0190 μs |  0.35 |    0.00 | 0.0038 |     400 B |        0.03 |
