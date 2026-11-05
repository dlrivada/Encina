```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.511 μs |  0.8303 μs | 0.0455 μs |  1.00 |    0.01 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  63.732 μs |  7.1720 μs | 0.3931 μs |  6.70 |    0.05 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 619.288 μs | 42.1498 μs | 2.3104 μs | 65.12 |    0.34 | 48.8281 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  50.029 μs |  8.6872 μs | 0.4762 μs |  5.26 |    0.05 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  66.435 μs |  3.1399 μs | 0.1721 μs |  6.99 |    0.03 |  5.7373 |      - |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  66.298 μs |  7.6487 μs | 0.4192 μs |  6.97 |    0.05 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  96.141 μs |  8.1798 μs | 0.4484 μs | 10.11 |    0.06 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.406 μs |  1.6564 μs | 0.0908 μs |  0.36 |    0.01 |  0.0229 |      - |     400 B |        0.03 |
