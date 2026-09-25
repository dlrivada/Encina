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
| Stream_SmallDataset_10Items             |   7.482 μs |  0.6984 μs | 0.0383 μs |  1.00 |    0.01 |  0.5417 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  53.355 μs | 10.8486 μs | 0.5946 μs |  7.13 |    0.08 |  3.4790 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 468.106 μs | 31.7763 μs | 1.7418 μs | 62.57 |    0.34 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  43.275 μs | 14.6580 μs | 0.8035 μs |  5.78 |    0.10 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  53.224 μs |  1.0882 μs | 0.0597 μs |  7.11 |    0.03 |  4.0894 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  50.405 μs |  2.0442 μs | 0.1120 μs |  6.74 |    0.03 |  3.4790 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       |  81.651 μs |  1.3580 μs | 0.0744 μs | 10.91 |    0.05 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   3.481 μs |  1.3944 μs | 0.0764 μs |  0.47 |    0.01 |  0.0229 |      - |     400 B |        0.04 |
