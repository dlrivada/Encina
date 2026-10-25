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
| Stream_SmallDataset_10Items             |   9.626 μs |  0.1758 μs | 0.0096 μs |  1.00 |    0.00 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  68.164 μs |  3.4995 μs | 0.1918 μs |  7.08 |    0.02 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 607.645 μs | 73.7954 μs | 4.0450 μs | 63.12 |    0.37 | 48.8281 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  51.940 μs |  5.5186 μs | 0.3025 μs |  5.40 |    0.03 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  69.943 μs |  6.0752 μs | 0.3330 μs |  7.27 |    0.03 |  5.7373 |      - |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  64.475 μs |  4.8373 μs | 0.2652 μs |  6.70 |    0.02 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  96.573 μs |  2.6851 μs | 0.1472 μs | 10.03 |    0.02 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.372 μs |  1.4174 μs | 0.0777 μs |  0.35 |    0.01 |  0.0229 |      - |     400 B |        0.03 |
