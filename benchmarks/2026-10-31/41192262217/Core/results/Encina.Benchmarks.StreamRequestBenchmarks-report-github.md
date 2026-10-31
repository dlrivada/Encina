```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  10.151 μs |  0.5172 μs | 0.0284 μs |  1.00 |    0.00 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  65.570 μs |  7.5901 μs | 0.4160 μs |  6.46 |    0.04 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 617.015 μs | 50.4315 μs | 2.7643 μs | 60.78 |    0.28 | 48.8281 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  51.621 μs | 10.1033 μs | 0.5538 μs |  5.09 |    0.05 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  69.596 μs |  5.9262 μs | 0.3248 μs |  6.86 |    0.03 |  5.7373 |      - |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  66.292 μs |  4.9065 μs | 0.2689 μs |  6.53 |    0.03 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 101.060 μs |  4.1503 μs | 0.2275 μs |  9.96 |    0.03 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.835 μs |  0.9273 μs | 0.0508 μs |  0.38 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
