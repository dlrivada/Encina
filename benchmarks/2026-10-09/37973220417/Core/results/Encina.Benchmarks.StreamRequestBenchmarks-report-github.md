```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.226 μs |  1.1372 μs | 0.0623 μs |  1.00 |    0.01 |  0.7629 |      - |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  62.511 μs |  8.4301 μs | 0.4621 μs |  6.78 |    0.06 |  5.1270 |      - |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 595.546 μs | 39.5858 μs | 2.1698 μs | 64.55 |    0.43 | 48.8281 |      - |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  51.217 μs |  3.6935 μs | 0.2025 μs |  5.55 |    0.04 |  3.0518 |      - |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  71.610 μs |  1.7753 μs | 0.0973 μs |  7.76 |    0.05 |  5.7373 |      - |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  63.450 μs |  5.1487 μs | 0.2822 μs |  6.88 |    0.05 |  5.1270 |      - |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  95.751 μs |  4.0256 μs | 0.2207 μs | 10.38 |    0.06 |  6.1035 | 0.1221 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   3.437 μs |  0.8421 μs | 0.0462 μs |  0.37 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
