```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.598 μs |  0.4655 μs | 0.0255 μs |  1.00 |    0.00 |  0.5341 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  63.555 μs |  7.7669 μs | 0.4257 μs |  6.62 |    0.04 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 605.755 μs | 47.7703 μs | 2.6184 μs | 63.11 |    0.28 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  54.622 μs |  2.7123 μs | 0.1487 μs |  5.69 |    0.02 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  67.094 μs |  5.2001 μs | 0.2850 μs |  6.99 |    0.03 |  4.0283 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  65.917 μs |  7.5123 μs | 0.4118 μs |  6.87 |    0.04 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       | 105.801 μs |  5.4377 μs | 0.2981 μs | 11.02 |    0.04 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.346 μs |  0.4402 μs | 0.0241 μs |  0.45 |    0.00 |  0.0229 |      - |     400 B |        0.04 |
