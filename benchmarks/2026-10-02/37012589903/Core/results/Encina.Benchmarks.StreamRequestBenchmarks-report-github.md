```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   7.440 μs |  0.9577 μs | 0.0525 μs |  1.00 |    0.01 |  0.5417 |      - |    9168 B |        1.00 |
| Stream_MediumDataset_100Items           |  49.849 μs |  2.0623 μs | 0.1130 μs |  6.70 |    0.04 |  3.4790 |      - |   58849 B |        6.42 |
| Stream_LargeDataset_1000Items           | 474.853 μs | 52.0625 μs | 2.8537 μs | 63.82 |    0.51 | 33.2031 |      - |  555656 B |       60.61 |
| Stream_WithPipelineBehaviors            |  42.147 μs |  0.8481 μs | 0.0465 μs |  5.66 |    0.03 |  2.1973 |      - |   36977 B |        4.03 |
| Stream_MaterializeToList_100Items       |  56.079 μs |  1.4122 μs | 0.0774 μs |  7.54 |    0.05 |  4.0894 |      - |   68665 B |        7.49 |
| Stream_CountOnly_NoMaterialization      |  49.891 μs |  2.5057 μs | 0.1373 μs |  6.71 |    0.04 |  3.4790 |      - |   58849 B |        6.42 |
| Stream_WithCancellation_EarlyExit       |  81.502 μs |  1.4977 μs | 0.0821 μs | 10.95 |    0.07 |  4.3945 | 0.1221 |   74625 B |        8.14 |
| Stream_DirectHandlerInvocation_NoEncina |   3.506 μs |  0.5954 μs | 0.0326 μs |  0.47 |    0.00 |  0.0229 |      - |     400 B |        0.04 |
