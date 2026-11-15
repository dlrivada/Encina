```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.538 μs | 0.0272 μs | 0.0381 μs |  1.00 |    0.00 |  0.5035 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  87.866 μs | 0.2417 μs | 0.3543 μs |  7.01 |    0.03 |  3.4180 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 844.393 μs | 1.6749 μs | 2.4550 μs | 67.35 |    0.28 | 32.2266 |  828632 B |       64.41 |
| Stream_WithPipelineBehaviors            |  73.486 μs | 0.2090 μs | 0.3063 μs |  5.86 |    0.03 |  1.9531 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  90.366 μs | 0.1703 μs | 0.2496 μs |  7.21 |    0.03 |  3.7842 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  88.206 μs | 0.3143 μs | 0.4507 μs |  7.03 |    0.04 |  3.4180 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 131.455 μs | 0.5176 μs | 0.7257 μs | 10.48 |    0.06 |  3.9063 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.275 μs | 0.0282 μs | 0.0413 μs |  0.34 |    0.00 |  0.0153 |     400 B |        0.03 |
