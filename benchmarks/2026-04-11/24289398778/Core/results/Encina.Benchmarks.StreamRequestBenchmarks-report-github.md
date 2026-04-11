```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                  | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|-----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   5.879 μs | 0.0108 μs | 0.0151 μs |   5.882 μs |  1.00 |    0.00 |  0.2518 |    4224 B |        1.00 |
| Stream_MediumDataset_100Items           |  35.203 μs | 0.0852 μs | 0.1249 μs |  35.211 μs |  5.99 |    0.03 |  1.2817 |   22224 B |        5.26 |
| Stream_LargeDataset_1000Items           | 324.231 μs | 1.4476 μs | 2.1219 μs | 323.731 μs | 55.15 |    0.38 | 11.7188 |  202227 B |       47.88 |
| Stream_WithPipelineBehaviors            |  38.335 μs | 0.0754 μs | 0.1082 μs |  38.328 μs |  6.52 |    0.02 |  1.0376 |   17952 B |        4.25 |
| Stream_MaterializeToList_100Items       |  38.463 μs | 0.5913 μs | 0.8480 μs |  37.839 μs |  6.54 |    0.14 |  1.8921 |   32040 B |        7.59 |
| Stream_CountOnly_NoMaterialization      |  35.244 μs | 0.1295 μs | 0.1857 μs |  35.250 μs |  5.99 |    0.03 |  1.2817 |   22224 B |        5.26 |
| Stream_WithCancellation_EarlyExit       |  69.819 μs | 0.1580 μs | 0.2215 μs |  69.862 μs | 11.88 |    0.05 |  1.8311 |   32600 B |        7.72 |
| Stream_DirectHandlerInvocation_NoEncina |   4.230 μs | 0.1475 μs | 0.2115 μs |   4.246 μs |  0.72 |    0.04 |  0.0229 |     400 B |        0.09 |
