```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.229 μs |  0.9372 μs | 0.0514 μs |  1.00 |    0.01 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  63.539 μs |  4.5608 μs | 0.2500 μs |  6.89 |    0.04 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 646.131 μs | 52.4217 μs | 2.8734 μs | 70.02 |    0.43 | 48.8281 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  49.855 μs |  6.4892 μs | 0.3557 μs |  5.40 |    0.04 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  66.879 μs |  4.0702 μs | 0.2231 μs |  7.25 |    0.04 |  5.7373 |      - |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  62.940 μs |  8.2146 μs | 0.4503 μs |  6.82 |    0.05 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  95.319 μs |  7.2497 μs | 0.3974 μs | 10.33 |    0.06 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.492 μs |  0.2152 μs | 0.0118 μs |  0.38 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
