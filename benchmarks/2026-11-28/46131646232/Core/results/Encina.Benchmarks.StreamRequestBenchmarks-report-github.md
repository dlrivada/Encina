```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.908 μs |  0.7324 μs | 0.0401 μs |  1.00 |    0.00 | 0.1526 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  70.289 μs | 11.4379 μs | 0.6269 μs |  7.09 |    0.06 | 0.9766 |   87024 B |        6.76 |
| Stream_LargeDataset_1000Items           | 670.900 μs | 30.5965 μs | 1.6771 μs | 67.71 |    0.28 | 9.7656 |  828626 B |       64.41 |
| Stream_WithPipelineBehaviors            |  51.766 μs |  7.2102 μs | 0.3952 μs |  5.22 |    0.04 | 0.6104 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  74.465 μs | 13.4973 μs | 0.7398 μs |  7.52 |    0.07 | 1.0986 |   96840 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  70.415 μs | 18.6572 μs | 1.0227 μs |  7.11 |    0.09 | 0.9766 |   87024 B |        6.76 |
| Stream_WithCancellation_EarlyExit       | 104.335 μs | 14.1517 μs | 0.7757 μs | 10.53 |    0.08 | 1.2207 |  102800 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.995 μs |  0.0688 μs | 0.0038 μs |  0.40 |    0.00 |      - |     400 B |        0.03 |
