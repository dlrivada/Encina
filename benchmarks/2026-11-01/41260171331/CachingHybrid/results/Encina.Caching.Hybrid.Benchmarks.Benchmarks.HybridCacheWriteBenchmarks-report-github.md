```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.548 μs | 0.0340 μs | 0.0487 μs |  0.70 |    0.03 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 8.395 μs | 0.2997 μs | 0.4298 μs |  2.32 |    0.14 | 0.1450 | 0.1373 |   2.39 KB |        2.30 |
| SetAsync                      | 3.623 μs | 0.0862 μs | 0.1208 μs |  1.00 |    0.05 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 3.854 μs | 0.1966 μs | 0.2882 μs |  1.06 |    0.09 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
