```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.49GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.419 μs | 0.5576 μs | 0.0306 μs |  0.79 |    0.01 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 6.300 μs | 2.3811 μs | 0.1305 μs |  2.06 |    0.04 | 0.1450 | 0.1373 |   2.39 KB |        2.30 |
| SetAsync                      | 3.061 μs | 0.4261 μs | 0.0234 μs |  1.00 |    0.01 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 3.062 μs | 0.3610 μs | 0.0198 μs |  1.00 |    0.01 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
