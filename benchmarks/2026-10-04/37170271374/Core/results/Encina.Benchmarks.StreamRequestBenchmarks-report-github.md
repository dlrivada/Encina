```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.789 μs | 0.1236 μs | 0.1732 μs |  1.00 |    0.02 |  0.5341 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  64.545 μs | 0.5245 μs | 0.7851 μs |  6.60 |    0.14 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 613.237 μs | 3.4089 μs | 4.7788 μs | 62.67 |    1.19 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  55.478 μs | 0.0899 μs | 0.1345 μs |  5.67 |    0.10 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  68.236 μs | 0.3064 μs | 0.4492 μs |  6.97 |    0.13 |  4.0283 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  64.291 μs | 0.4305 μs | 0.6311 μs |  6.57 |    0.13 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       | 107.985 μs | 0.6408 μs | 0.9591 μs | 11.03 |    0.22 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.091 μs | 0.0184 μs | 0.0269 μs |  0.42 |    0.01 |  0.0229 |      - |     400 B |        0.04 |
