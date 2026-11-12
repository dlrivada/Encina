```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.552 μs | 0.7906 μs | 0.0433 μs |  0.80 |    0.05 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 6.637 μs | 0.9816 μs | 0.0538 μs |  2.07 |    0.13 | 0.1450 | 0.1373 |   2.39 KB |        2.30 |
| SetAsync                      | 3.212 μs | 4.3363 μs | 0.2377 μs |  1.00 |    0.09 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 3.302 μs | 0.8374 μs | 0.0459 μs |  1.03 |    0.07 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
