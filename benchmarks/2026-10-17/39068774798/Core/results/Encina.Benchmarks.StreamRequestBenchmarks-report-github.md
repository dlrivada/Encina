```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   6.262 μs |   1.3358 μs |  0.0732 μs |  1.00 |    0.01 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  43.285 μs |  32.9443 μs |  1.8058 μs |  6.91 |    0.26 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 416.156 μs | 442.1586 μs | 24.2362 μs | 66.47 |    3.42 | 49.3164 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  39.207 μs |  22.0177 μs |  1.2069 μs |  6.26 |    0.18 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  44.402 μs |  13.6267 μs |  0.7469 μs |  7.09 |    0.13 |  5.7373 | 0.0610 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  42.288 μs |  27.1736 μs |  1.4895 μs |  6.75 |    0.22 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  65.279 μs |   3.7793 μs |  0.2072 μs | 10.43 |    0.11 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   2.196 μs |   0.5304 μs |  0.0291 μs |  0.35 |    0.01 |  0.0229 |      - |     400 B |        0.03 |
