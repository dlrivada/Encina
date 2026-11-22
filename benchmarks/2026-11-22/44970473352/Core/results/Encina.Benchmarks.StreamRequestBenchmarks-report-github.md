```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.398 μs | 0.0312 μs | 0.0467 μs |  1.00 |    0.01 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  63.050 μs | 0.2865 μs | 0.4289 μs |  6.71 |    0.06 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 592.402 μs | 3.8427 μs | 5.7516 μs | 63.03 |    0.68 | 48.8281 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  49.615 μs | 0.1958 μs | 0.2808 μs |  5.28 |    0.04 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  66.699 μs | 0.6632 μs | 0.9926 μs |  7.10 |    0.11 |  5.7373 |      - |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  62.694 μs | 0.2426 μs | 0.3631 μs |  6.67 |    0.05 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  95.258 μs | 1.1012 μs | 1.6141 μs | 10.14 |    0.18 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.420 μs | 0.0204 μs | 0.0300 μs |  0.36 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
