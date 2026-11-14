```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.095 μs |   0.9448 μs |  0.0518 μs |  1.00 |    0.01 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  80.970 μs |   3.7600 μs |  0.2061 μs |  6.69 |    0.03 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 764.344 μs | 199.5841 μs | 10.9399 μs | 63.20 |    0.82 | 48.8281 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  65.396 μs |   5.8770 μs |  0.3221 μs |  5.41 |    0.03 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  86.328 μs |   6.0660 μs |  0.3325 μs |  7.14 |    0.04 |  5.7373 |      - |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  82.921 μs |  13.1175 μs |  0.7190 μs |  6.86 |    0.06 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 120.253 μs |  11.0510 μs |  0.6057 μs |  9.94 |    0.06 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.188 μs |   0.9286 μs |  0.0509 μs |  0.35 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
