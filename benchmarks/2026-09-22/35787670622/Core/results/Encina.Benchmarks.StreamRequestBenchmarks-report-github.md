```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   4.962 μs |  0.4328 μs | 0.0237 μs |  1.00 |    0.01 |  0.2518 |    4224 B |        1.00 |
| Stream_MediumDataset_100Items           |  28.509 μs |  5.9412 μs | 0.3257 μs |  5.75 |    0.06 |  1.3123 |   22224 B |        5.26 |
| Stream_LargeDataset_1000Items           | 247.456 μs | 18.5027 μs | 1.0142 μs | 49.87 |    0.27 | 11.7188 |  202227 B |       47.88 |
| Stream_WithPipelineBehaviors            |  30.204 μs |  1.6070 μs | 0.0881 μs |  6.09 |    0.03 |  1.0681 |   17952 B |        4.25 |
| Stream_MaterializeToList_100Items       |  29.900 μs |  2.2860 μs | 0.1253 μs |  6.03 |    0.03 |  1.8921 |   32040 B |        7.59 |
| Stream_CountOnly_NoMaterialization      |  26.628 μs |  2.7301 μs | 0.1496 μs |  5.37 |    0.03 |  1.3123 |   22224 B |        5.26 |
| Stream_WithCancellation_EarlyExit       |  52.549 μs |  3.0286 μs | 0.1660 μs | 10.59 |    0.05 |  1.8921 |   32600 B |        7.72 |
| Stream_DirectHandlerInvocation_NoEncina |   3.470 μs |  0.3714 μs | 0.0204 μs |  0.70 |    0.00 |  0.0229 |     400 B |        0.09 |
