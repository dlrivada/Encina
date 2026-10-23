```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.540 μs | 0.2475 μs | 0.0136 μs |  0.80 |    0.02 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 6.719 μs | 2.2405 μs | 0.1228 μs |  2.10 |    0.06 | 0.1450 | 0.1373 |   2.39 KB |        2.30 |
| SetAsync                      | 3.195 μs | 1.5093 μs | 0.0827 μs |  1.00 |    0.03 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 3.341 μs | 1.3917 μs | 0.0763 μs |  1.05 |    0.03 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
