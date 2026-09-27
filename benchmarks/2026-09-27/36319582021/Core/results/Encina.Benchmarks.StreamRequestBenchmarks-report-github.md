```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.677 μs |  0.8493 μs | 0.0466 μs |  1.00 |    0.01 |  0.5341 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  64.161 μs |  2.5642 μs | 0.1406 μs |  6.63 |    0.03 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 621.153 μs | 82.4202 μs | 4.5177 μs | 64.19 |    0.48 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  56.275 μs |  5.4238 μs | 0.2973 μs |  5.82 |    0.04 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  66.513 μs |  4.2191 μs | 0.2313 μs |  6.87 |    0.04 |  4.0283 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  64.535 μs |  4.0229 μs | 0.2205 μs |  6.67 |    0.03 |  3.4180 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       | 108.400 μs |  5.1233 μs | 0.2808 μs | 11.20 |    0.05 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   4.070 μs |  0.3303 μs | 0.0181 μs |  0.42 |    0.00 |  0.0229 |      - |     400 B |        0.04 |
