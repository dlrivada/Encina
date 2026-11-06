```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.718 μs | 0.2309 μs | 0.0127 μs |  0.92 |    0.01 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 8.701 μs | 4.1666 μs | 0.2284 μs |  2.14 |    0.05 | 0.1678 | 0.1526 |   2.97 KB |        2.86 |
| SetAsync                      | 4.060 μs | 0.9981 μs | 0.0547 μs |  1.00 |    0.02 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 4.077 μs | 2.9191 μs | 0.1600 μs |  1.00 |    0.04 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
