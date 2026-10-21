```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.476 μs |  0.7513 μs | 0.0412 μs |  1.00 |    0.00 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  82.637 μs |  5.6963 μs | 0.3122 μs |  6.62 |    0.03 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 779.125 μs | 70.6264 μs | 3.8713 μs | 62.45 |    0.32 | 48.8281 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  64.559 μs |  3.8904 μs | 0.2132 μs |  5.17 |    0.02 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  85.942 μs | 23.9522 μs | 1.3129 μs |  6.89 |    0.09 |  5.7373 |      - |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  84.969 μs |  9.2339 μs | 0.5061 μs |  6.81 |    0.04 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 121.505 μs | 19.0945 μs | 1.0466 μs |  9.74 |    0.08 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.159 μs |  0.5312 μs | 0.0291 μs |  0.33 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
