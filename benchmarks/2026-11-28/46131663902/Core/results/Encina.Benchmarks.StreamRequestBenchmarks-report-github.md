```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   7.076 μs |  0.4625 μs | 0.0254 μs |  1.00 |    0.00 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  42.230 μs |  5.0940 μs | 0.2792 μs |  5.97 |    0.04 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 396.208 μs |  6.7561 μs | 0.3703 μs | 55.99 |    0.18 | 49.3164 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  38.732 μs | 51.4539 μs | 2.8204 μs |  5.47 |    0.35 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  46.593 μs |  6.6578 μs | 0.3649 μs |  6.58 |    0.05 |  5.7373 | 0.0610 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  44.039 μs | 32.2244 μs | 1.7663 μs |  6.22 |    0.22 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  64.810 μs | 31.7299 μs | 1.7392 μs |  9.16 |    0.21 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   2.402 μs |  1.2665 μs | 0.0694 μs |  0.34 |    0.01 |  0.0229 |      - |     400 B |        0.03 |
