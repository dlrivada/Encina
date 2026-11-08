```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                  | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|-----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.374 μs | 0.0377 μs | 0.0553 μs |  12.368 μs |  1.00 |    0.01 |  0.5035 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  87.752 μs | 0.4631 μs | 0.6493 μs |  87.638 μs |  7.09 |    0.06 |  3.4180 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 838.520 μs | 4.7312 μs | 6.7853 μs | 839.452 μs | 67.77 |    0.62 | 32.2266 |  828632 B |       64.41 |
| Stream_WithPipelineBehaviors            |  72.153 μs | 0.6796 μs | 0.9747 μs |  71.520 μs |  5.83 |    0.08 |  1.9531 |   51552 B |        4.01 |
| Stream_MaterializeToList_100Items       |  91.413 μs | 0.3673 μs | 0.5148 μs |  91.330 μs |  7.39 |    0.05 |  3.7842 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  88.159 μs | 0.2228 μs | 0.3195 μs |  88.189 μs |  7.12 |    0.04 |  3.4180 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 127.367 μs | 0.1902 μs | 0.2847 μs | 127.308 μs | 10.29 |    0.05 |  3.9063 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.556 μs | 0.0294 μs | 0.0421 μs |   4.553 μs |  0.37 |    0.00 |  0.0153 |     400 B |        0.03 |
