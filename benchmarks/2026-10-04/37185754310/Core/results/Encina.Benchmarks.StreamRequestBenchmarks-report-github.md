```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   7.393 μs | 0.3983 μs | 0.0218 μs |  1.00 |    0.00 |  0.5417 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  50.883 μs | 6.3615 μs | 0.3487 μs |  6.88 |    0.04 |  3.4790 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 493.480 μs | 8.6573 μs | 0.4745 μs | 66.75 |    0.18 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  43.989 μs | 1.4939 μs | 0.0819 μs |  5.95 |    0.02 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  53.157 μs | 6.2281 μs | 0.3414 μs |  7.19 |    0.04 |  4.0894 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  50.638 μs | 2.3797 μs | 0.1304 μs |  6.85 |    0.02 |  3.4790 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       |  81.611 μs | 4.4552 μs | 0.2442 μs | 11.04 |    0.04 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   3.512 μs | 0.6655 μs | 0.0365 μs |  0.48 |    0.00 |  0.0229 |      - |     400 B |        0.04 |
