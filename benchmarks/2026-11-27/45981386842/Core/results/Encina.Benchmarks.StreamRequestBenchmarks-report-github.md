```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  10.041 μs |  0.9658 μs | 0.0529 μs |  1.00 |    0.01 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  66.666 μs |  8.6374 μs | 0.4734 μs |  6.64 |    0.05 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 615.689 μs | 77.1173 μs | 4.2271 μs | 61.32 |    0.46 | 48.8281 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  52.203 μs |  4.3169 μs | 0.2366 μs |  5.20 |    0.03 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  69.079 μs | 14.3526 μs | 0.7867 μs |  6.88 |    0.07 |  5.7373 |      - |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  66.374 μs | 28.7285 μs | 1.5747 μs |  6.61 |    0.14 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 101.221 μs | 16.6153 μs | 0.9107 μs | 10.08 |    0.09 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.435 μs |  0.6626 μs | 0.0363 μs |  0.34 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
