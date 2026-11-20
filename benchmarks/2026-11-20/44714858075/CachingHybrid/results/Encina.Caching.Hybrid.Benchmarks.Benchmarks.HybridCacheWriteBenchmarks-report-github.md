```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.736 μs | 0.6459 μs | 0.0354 μs |  0.77 |    0.01 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 7.269 μs | 7.9805 μs | 0.4374 μs |  2.05 |    0.11 | 0.1678 | 0.1526 |   2.97 KB |        2.86 |
| SetAsync                      | 3.540 μs | 0.9793 μs | 0.0537 μs |  1.00 |    0.02 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 3.552 μs | 2.1358 μs | 0.1171 μs |  1.00 |    0.03 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
