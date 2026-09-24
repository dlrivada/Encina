```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   5.412 μs |   0.5091 μs |  0.0279 μs |  1.00 |    0.01 |  0.5417 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  35.104 μs |  28.8905 μs |  1.5836 μs |  6.49 |    0.26 |  3.4790 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 329.681 μs | 374.8238 μs | 20.5454 μs | 60.92 |    3.30 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  31.756 μs |   5.7676 μs |  0.3161 μs |  5.87 |    0.06 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  37.042 μs |   3.0844 μs |  0.1691 μs |  6.84 |    0.04 |  4.0894 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  33.953 μs |   5.6267 μs |  0.3084 μs |  6.27 |    0.06 |  3.4790 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       |  56.901 μs |   7.2836 μs |  0.3992 μs | 10.51 |    0.08 |  4.4556 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   2.295 μs |   0.1950 μs |  0.0107 μs |  0.42 |    0.00 |  0.0229 |      - |     400 B |        0.04 |
