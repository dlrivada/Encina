```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  11.678 μs |  0.5488 μs | 0.0301 μs |  1.00 |    0.00 |  0.7629 |      - |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  79.540 μs |  7.4777 μs | 0.4099 μs |  6.81 |    0.03 |  5.1270 |      - |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 741.686 μs | 34.4835 μs | 1.8902 μs | 63.51 |    0.20 | 48.8281 |      - |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  63.537 μs |  3.0870 μs | 0.1692 μs |  5.44 |    0.02 |  3.0518 |      - |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  86.084 μs |  9.7490 μs | 0.5344 μs |  7.37 |    0.04 |  5.7373 |      - |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  81.670 μs |  6.5284 μs | 0.3578 μs |  6.99 |    0.03 |  5.1270 |      - |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 120.004 μs |  6.8255 μs | 0.3741 μs | 10.28 |    0.04 |  6.1035 | 0.1221 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   4.321 μs |  1.0642 μs | 0.0583 μs |  0.37 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
