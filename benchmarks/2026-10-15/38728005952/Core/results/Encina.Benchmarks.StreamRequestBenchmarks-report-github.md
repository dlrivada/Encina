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
| Stream_SmallDataset_10Items             |   9.631 μs |  1.1290 μs | 0.0619 μs |  1.00 |    0.01 |  0.7629 |      - |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  65.186 μs |  2.8786 μs | 0.1578 μs |  6.77 |    0.04 |  5.1270 |      - |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 624.533 μs | 86.1229 μs | 4.7207 μs | 64.85 |    0.56 | 48.8281 |      - |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  53.431 μs |  3.4617 μs | 0.1897 μs |  5.55 |    0.04 |  3.0518 |      - |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  66.647 μs |  7.1936 μs | 0.3943 μs |  6.92 |    0.05 |  5.7373 |      - |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  64.608 μs |  2.1362 μs | 0.1171 μs |  6.71 |    0.04 |  5.1270 |      - |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  97.476 μs |  7.0692 μs | 0.3875 μs | 10.12 |    0.07 |  6.1035 | 0.1221 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   3.410 μs |  0.3656 μs | 0.0200 μs |  0.35 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
