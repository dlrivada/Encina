```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.88GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   8.072 μs |   0.4808 μs | 0.0264 μs |  1.00 |    0.00 | 0.1526 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  58.118 μs |  34.4856 μs | 1.8903 μs |  7.20 |    0.20 | 1.0376 |   87024 B |        6.76 |
| Stream_LargeDataset_1000Items           | 567.099 μs | 146.7279 μs | 8.0427 μs | 70.25 |    0.89 | 9.7656 |  828626 B |       64.41 |
| Stream_WithPipelineBehaviors            |  43.489 μs |  47.7348 μs | 2.6165 μs |  5.39 |    0.28 | 0.6104 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  59.646 μs |  10.9364 μs | 0.5995 μs |  7.39 |    0.07 | 1.0986 |   96840 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  62.599 μs |  18.8889 μs | 1.0354 μs |  7.75 |    0.11 | 1.0376 |   87024 B |        6.76 |
| Stream_WithCancellation_EarlyExit       |  84.442 μs |  54.8207 μs | 3.0049 μs | 10.46 |    0.32 | 1.2207 |  102800 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   2.908 μs |   0.2632 μs | 0.0144 μs |  0.36 |    0.00 | 0.0038 |     400 B |        0.03 |
