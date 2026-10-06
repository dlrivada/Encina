```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   6.203 μs |  0.7412 μs | 0.0406 μs |  1.00 |    0.01 |  0.7629 |      - |   12848 B |        1.00 |
| Stream_MediumDataset_100Items           |  40.250 μs |  6.7731 μs | 0.3713 μs |  6.49 |    0.06 |  5.1880 |      - |   87009 B |        6.77 |
| Stream_LargeDataset_1000Items           | 381.449 μs | 36.2509 μs | 1.9870 μs | 61.49 |    0.45 | 49.3164 | 0.4883 |  828620 B |       64.49 |
| Stream_WithPipelineBehaviors            |  35.745 μs |  9.4881 μs | 0.5201 μs |  5.76 |    0.08 |  3.0518 |      - |   51537 B |        4.01 |
| Stream_MaterializeToList_100Items       |  43.042 μs | 28.4937 μs | 1.5618 μs |  6.94 |    0.22 |  5.7373 | 0.0610 |   96825 B |        7.54 |
| Stream_CountOnly_NoMaterialization      |  40.582 μs |  8.4344 μs | 0.4623 μs |  6.54 |    0.07 |  5.1880 |      - |   87009 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  65.735 μs |  2.4974 μs | 0.1369 μs | 10.60 |    0.06 |  6.1035 | 0.1221 |  102785 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   2.237 μs |  0.1105 μs | 0.0061 μs |  0.36 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
