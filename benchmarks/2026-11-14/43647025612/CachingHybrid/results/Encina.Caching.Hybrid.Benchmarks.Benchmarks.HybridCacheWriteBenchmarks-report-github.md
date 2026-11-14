```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.42GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.489 μs | 0.1037 μs | 0.0057 μs |  0.76 |    0.01 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 7.344 μs | 5.6626 μs | 0.3104 μs |  2.24 |    0.09 | 0.1450 | 0.1373 |   2.39 KB |        2.30 |
| SetAsync                      | 3.281 μs | 1.0632 μs | 0.0583 μs |  1.00 |    0.02 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 3.285 μs | 1.8517 μs | 0.1015 μs |  1.00 |    0.03 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
