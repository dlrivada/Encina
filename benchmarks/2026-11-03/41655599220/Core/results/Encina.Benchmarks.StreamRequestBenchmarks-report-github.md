```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.396 μs |  0.5006 μs | 0.0274 μs |  1.00 |    0.00 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  83.420 μs |  5.4657 μs | 0.2996 μs |  6.73 |    0.02 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 765.875 μs | 58.2830 μs | 3.1947 μs | 61.78 |    0.25 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  65.632 μs |  2.0995 μs | 0.1151 μs |  5.29 |    0.01 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  87.300 μs |  7.4285 μs | 0.4072 μs |  7.04 |    0.03 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  83.443 μs | 12.3425 μs | 0.6765 μs |  6.73 |    0.05 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 123.529 μs |  1.1478 μs | 0.0629 μs |  9.97 |    0.02 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.226 μs |  1.1173 μs | 0.0612 μs |  0.34 |    0.00 |  0.0229 |     400 B |        0.03 |
