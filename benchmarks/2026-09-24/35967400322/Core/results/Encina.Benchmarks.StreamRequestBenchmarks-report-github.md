```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.84GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.773 μs |  0.3944 μs | 0.0216 μs |  1.00 |    0.00 |  0.5341 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  66.337 μs |  3.9556 μs | 0.2168 μs |  6.79 |    0.02 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 612.861 μs | 40.4661 μs | 2.2181 μs | 62.71 |    0.23 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  56.861 μs |  1.0855 μs | 0.0595 μs |  5.82 |    0.01 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  68.504 μs |  4.9887 μs | 0.2734 μs |  7.01 |    0.03 |  4.0283 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  66.201 μs |  1.2887 μs | 0.0706 μs |  6.77 |    0.01 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       | 112.996 μs |  6.8164 μs | 0.3736 μs | 11.56 |    0.04 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.051 μs |  0.4304 μs | 0.0236 μs |  0.41 |    0.00 |  0.0229 |      - |     400 B |        0.04 |
