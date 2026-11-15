```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.409 μs | 0.0164 μs | 0.0240 μs |  0.72 |    0.02 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 7.231 μs | 0.2381 μs | 0.3415 μs |  2.15 |    0.12 | 0.1450 | 0.1373 |   2.39 KB |        2.30 |
| SetAsync                      | 3.366 μs | 0.0788 μs | 0.1105 μs |  1.00 |    0.05 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 3.497 μs | 0.2409 μs | 0.3605 μs |  1.04 |    0.11 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
