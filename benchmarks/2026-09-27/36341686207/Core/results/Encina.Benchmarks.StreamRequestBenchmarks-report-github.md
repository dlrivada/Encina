```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.882 μs |  0.4480 μs | 0.0246 μs |  1.00 |    0.00 |  0.5341 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  64.544 μs | 18.3524 μs | 1.0060 μs |  6.53 |    0.09 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 602.430 μs | 25.6921 μs | 1.4083 μs | 60.96 |    0.18 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  57.282 μs |  8.6864 μs | 0.4761 μs |  5.80 |    0.04 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  68.206 μs | 27.6747 μs | 1.5169 μs |  6.90 |    0.13 |  4.0283 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  63.865 μs | 14.4029 μs | 0.7895 μs |  6.46 |    0.07 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       | 109.547 μs |  7.6258 μs | 0.4180 μs | 11.09 |    0.04 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.115 μs |  0.9054 μs | 0.0496 μs |  0.42 |    0.00 |  0.0229 |      - |     400 B |        0.04 |
