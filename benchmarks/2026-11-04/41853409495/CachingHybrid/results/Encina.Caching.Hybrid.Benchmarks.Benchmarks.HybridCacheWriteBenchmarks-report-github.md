```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.40GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.439 μs | 0.2287 μs | 0.0125 μs |  0.75 |    0.01 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 6.719 μs | 2.4303 μs | 0.1332 μs |  2.06 |    0.04 | 0.1450 | 0.1373 |   2.39 KB |        2.30 |
| SetAsync                      | 3.261 μs | 0.9220 μs | 0.0505 μs |  1.00 |    0.02 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 3.234 μs | 0.3885 μs | 0.0213 μs |  0.99 |    0.01 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
