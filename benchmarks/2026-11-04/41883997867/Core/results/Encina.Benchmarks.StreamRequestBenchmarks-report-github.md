```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   7.110 μs |   2.035 μs |  0.1115 μs |  1.00 |    0.02 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  45.553 μs |  10.323 μs |  0.5658 μs |  6.41 |    0.11 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 435.852 μs | 206.988 μs | 11.3457 μs | 61.31 |    1.61 | 49.3164 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  39.831 μs |  28.635 μs |  1.5696 μs |  5.60 |    0.21 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  46.268 μs |   6.099 μs |  0.3343 μs |  6.51 |    0.10 |  5.7373 | 0.0610 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  43.480 μs |   7.628 μs |  0.4181 μs |  6.12 |    0.10 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  67.515 μs |  10.970 μs |  0.6013 μs |  9.50 |    0.15 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   2.273 μs |   1.591 μs |  0.0872 μs |  0.32 |    0.01 |  0.0229 |      - |     400 B |        0.03 |
