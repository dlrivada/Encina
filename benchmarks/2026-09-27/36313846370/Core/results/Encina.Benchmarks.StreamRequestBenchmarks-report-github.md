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
| Stream_SmallDataset_10Items             |   7.435 μs |  0.3955 μs | 0.0217 μs |  1.00 |    0.00 |  0.5417 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  51.330 μs |  5.1148 μs | 0.2804 μs |  6.90 |    0.04 |  3.4790 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 476.535 μs | 31.2752 μs | 1.7143 μs | 64.09 |    0.26 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  43.297 μs |  4.0564 μs | 0.2223 μs |  5.82 |    0.03 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  53.138 μs |  9.8331 μs | 0.5390 μs |  7.15 |    0.07 |  4.0894 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  50.731 μs |  8.6436 μs | 0.4738 μs |  6.82 |    0.06 |  3.4790 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       |  83.182 μs | 11.8333 μs | 0.6486 μs | 11.19 |    0.08 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   3.505 μs |  0.2875 μs | 0.0158 μs |  0.47 |    0.00 |  0.0229 |      - |     400 B |        0.04 |
