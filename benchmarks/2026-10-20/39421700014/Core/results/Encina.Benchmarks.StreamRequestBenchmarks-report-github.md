```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.01GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  11.222 μs |  0.3341 μs | 0.0183 μs |  1.00 |    0.00 | 0.1526 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  78.572 μs |  8.8662 μs | 0.4860 μs |  7.00 |    0.04 | 0.9766 |   87024 B |        6.76 |
| Stream_LargeDataset_1000Items           | 750.553 μs | 47.2323 μs | 2.5890 μs | 66.88 |    0.22 | 9.7656 |  828626 B |       64.41 |
| Stream_WithPipelineBehaviors            |  57.029 μs |  1.3899 μs | 0.0762 μs |  5.08 |    0.01 | 0.6104 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  81.932 μs |  1.4713 μs | 0.0806 μs |  7.30 |    0.01 | 1.0986 |   96840 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  78.250 μs |  5.7724 μs | 0.3164 μs |  6.97 |    0.03 | 0.9766 |   87024 B |        6.76 |
| Stream_WithCancellation_EarlyExit       | 114.356 μs |  5.0857 μs | 0.2788 μs | 10.19 |    0.03 | 1.2207 |  102800 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.473 μs |  0.4093 μs | 0.0224 μs |  0.40 |    0.00 |      - |     400 B |        0.03 |
