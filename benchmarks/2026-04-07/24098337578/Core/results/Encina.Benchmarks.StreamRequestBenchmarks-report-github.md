```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   6.133 μs | 0.0353 μs | 0.0506 μs |  1.00 |    0.01 |  0.2518 |    4224 B |        1.00 |
| Stream_MediumDataset_100Items           |  34.967 μs | 0.0811 μs | 0.1164 μs |  5.70 |    0.05 |  1.2817 |   22224 B |        5.26 |
| Stream_LargeDataset_1000Items           | 326.502 μs | 0.9418 μs | 1.3202 μs | 53.24 |    0.48 | 11.7188 |  202227 B |       47.88 |
| Stream_WithPipelineBehaviors            |  38.664 μs | 0.1382 μs | 0.2069 μs |  6.30 |    0.06 |  1.0376 |   17952 B |        4.25 |
| Stream_MaterializeToList_100Items       |  38.337 μs | 0.2125 μs | 0.3180 μs |  6.25 |    0.07 |  1.8921 |   32040 B |        7.59 |
| Stream_CountOnly_NoMaterialization      |  35.409 μs | 0.1410 μs | 0.2111 μs |  5.77 |    0.06 |  1.2817 |   22224 B |        5.26 |
| Stream_WithCancellation_EarlyExit       |  69.346 μs | 0.1166 μs | 0.1672 μs | 11.31 |    0.10 |  1.8311 |   32600 B |        7.72 |
| Stream_DirectHandlerInvocation_NoEncina |   4.237 μs | 0.0232 μs | 0.0339 μs |  0.69 |    0.01 |  0.0229 |     400 B |        0.09 |
