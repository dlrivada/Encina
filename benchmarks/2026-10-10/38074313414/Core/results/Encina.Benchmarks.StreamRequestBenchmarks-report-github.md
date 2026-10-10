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
| Stream_SmallDataset_10Items             |   9.128 μs |  1.2873 μs | 0.0706 μs |  1.00 |    0.01 |  0.7629 |      - |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  61.910 μs |  6.7093 μs | 0.3678 μs |  6.78 |    0.06 |  5.1270 |      - |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 588.196 μs | 36.0280 μs | 1.9748 μs | 64.44 |    0.47 | 48.8281 |      - |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  50.210 μs |  6.0217 μs | 0.3301 μs |  5.50 |    0.05 |  3.0518 |      - |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  67.474 μs |  5.5847 μs | 0.3061 μs |  7.39 |    0.06 |  5.7373 |      - |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  63.443 μs |  3.7962 μs | 0.2081 μs |  6.95 |    0.05 |  5.1270 |      - |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  96.425 μs | 19.2413 μs | 1.0547 μs | 10.56 |    0.12 |  6.1035 | 0.1221 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   3.443 μs |  0.2285 μs | 0.0125 μs |  0.38 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
