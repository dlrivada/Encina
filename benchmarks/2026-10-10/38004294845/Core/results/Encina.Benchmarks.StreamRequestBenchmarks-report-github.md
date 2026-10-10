```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.228 μs |  0.9093 μs | 0.0498 μs |  1.00 |    0.00 |  0.7629 |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  82.523 μs |  9.4483 μs | 0.5179 μs |  6.75 |    0.04 |  5.1270 |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 756.974 μs | 40.7650 μs | 2.2345 μs | 61.90 |    0.27 | 48.8281 |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  65.725 μs |  1.8672 μs | 0.1023 μs |  5.37 |    0.02 |  3.0518 |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  86.432 μs |  5.4776 μs | 0.3002 μs |  7.07 |    0.03 |  5.7373 |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  81.886 μs | 36.2515 μs | 1.9871 μs |  6.70 |    0.14 |  5.1270 |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 127.014 μs |  5.0560 μs | 0.2771 μs | 10.39 |    0.04 |  6.1035 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   4.101 μs |  0.4927 μs | 0.0270 μs |  0.34 |    0.00 |  0.0229 |     400 B |        0.03 |
