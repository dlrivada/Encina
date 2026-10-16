```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.315 μs |  1.1331 μs | 0.0621 μs |  1.00 |    0.01 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  62.487 μs | 16.5155 μs | 0.9053 μs |  6.71 |    0.09 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 585.891 μs | 49.0248 μs | 2.6872 μs | 62.90 |    0.44 | 48.8281 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  50.022 μs |  4.3076 μs | 0.2361 μs |  5.37 |    0.04 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  64.896 μs |  4.7678 μs | 0.2613 μs |  6.97 |    0.05 |  5.7373 |      - |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  62.353 μs |  4.0757 μs | 0.2234 μs |  6.69 |    0.04 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  96.620 μs | 10.4257 μs | 0.5715 μs | 10.37 |    0.08 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.286 μs |  0.5844 μs | 0.0320 μs |  0.35 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
