```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   7.512 μs |  0.5011 μs | 0.0275 μs |  1.00 |    0.00 |  0.5417 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  50.044 μs |  4.9779 μs | 0.2729 μs |  6.66 |    0.04 |  3.4790 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 467.025 μs | 12.4543 μs | 0.6827 μs | 62.17 |    0.21 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  42.775 μs |  3.2609 μs | 0.1787 μs |  5.69 |    0.03 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  54.916 μs |  0.7134 μs | 0.0391 μs |  7.31 |    0.02 |  4.0894 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  50.004 μs |  4.3268 μs | 0.2372 μs |  6.66 |    0.03 |  3.4790 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       |  81.623 μs |  4.0829 μs | 0.2238 μs | 10.87 |    0.04 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   3.493 μs |  0.5163 μs | 0.0283 μs |  0.47 |    0.00 |  0.0229 |      - |     400 B |        0.04 |
