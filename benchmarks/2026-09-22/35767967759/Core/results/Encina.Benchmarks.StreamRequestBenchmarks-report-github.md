```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   4.720 μs |  0.3397 μs | 0.0186 μs |  1.00 |    0.00 |  0.2518 |    4224 B |        1.00 |
| Stream_MediumDataset_100Items           |  29.042 μs |  2.0067 μs | 0.1100 μs |  6.15 |    0.03 |  1.3123 |   22224 B |        5.26 |
| Stream_LargeDataset_1000Items           | 243.500 μs | 31.8500 μs | 1.7458 μs | 51.59 |    0.37 | 11.7188 |  202227 B |       47.88 |
| Stream_WithPipelineBehaviors            |  30.592 μs |  0.4164 μs | 0.0228 μs |  6.48 |    0.02 |  1.0376 |   17952 B |        4.25 |
| Stream_MaterializeToList_100Items       |  29.782 μs |  3.8862 μs | 0.2130 μs |  6.31 |    0.04 |  1.8921 |   32040 B |        7.59 |
| Stream_CountOnly_NoMaterialization      |  29.253 μs |  2.9382 μs | 0.1611 μs |  6.20 |    0.04 |  1.3123 |   22224 B |        5.26 |
| Stream_WithCancellation_EarlyExit       |  52.625 μs |  1.7139 μs | 0.0939 μs | 11.15 |    0.04 |  1.8921 |   32600 B |        7.72 |
| Stream_DirectHandlerInvocation_NoEncina |   3.423 μs |  0.7342 μs | 0.0402 μs |  0.73 |    0.01 |  0.0229 |     400 B |        0.09 |
