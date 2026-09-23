```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   3.921 μs |  0.6700 μs | 0.0367 μs |  1.00 |    0.01 | 0.0458 |    4224 B |        1.00 |
| Stream_MediumDataset_100Items           |  23.658 μs | 21.8134 μs | 1.1957 μs |  6.03 |    0.27 | 0.2441 |   22224 B |        5.26 |
| Stream_LargeDataset_1000Items           | 213.853 μs | 38.8304 μs | 2.1284 μs | 54.55 |    0.64 | 2.1973 |  202225 B |       47.88 |
| Stream_WithPipelineBehaviors            |  22.714 μs |  0.7500 μs | 0.0411 μs |  5.79 |    0.05 | 0.2136 |   17952 B |        4.25 |
| Stream_MaterializeToList_100Items       |  24.583 μs |  2.1107 μs | 0.1157 μs |  6.27 |    0.06 | 0.3662 |   32040 B |        7.59 |
| Stream_CountOnly_NoMaterialization      |  22.616 μs |  0.4778 μs | 0.0262 μs |  5.77 |    0.05 | 0.2441 |   22224 B |        5.26 |
| Stream_WithCancellation_EarlyExit       |  42.258 μs |  4.3472 μs | 0.2383 μs | 10.78 |    0.10 | 0.3662 |   32600 B |        7.72 |
| Stream_DirectHandlerInvocation_NoEncina |   2.876 μs |  0.1398 μs | 0.0077 μs |  0.73 |    0.01 | 0.0038 |     400 B |        0.09 |
