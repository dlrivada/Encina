```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.442 μs | 0.0689 μs | 0.0038 μs |  0.77 |    0.03 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 6.411 μs | 3.9208 μs | 0.2149 μs |  2.03 |    0.11 | 0.1450 | 0.1373 |   2.39 KB |        2.30 |
| SetAsync                      | 3.158 μs | 3.0356 μs | 0.1664 μs |  1.00 |    0.06 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 3.048 μs | 1.5303 μs | 0.0839 μs |  0.97 |    0.05 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
