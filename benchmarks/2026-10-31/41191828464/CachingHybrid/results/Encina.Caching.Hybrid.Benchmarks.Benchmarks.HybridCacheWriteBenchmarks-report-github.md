```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   |  4.985 μs | 1.2617 μs | 0.0692 μs |  1.07 |    0.02 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 10.562 μs | 1.3301 μs | 0.0729 μs |  2.27 |    0.03 | 0.1678 | 0.1526 |   2.97 KB |        2.86 |
| SetAsync                      |  4.660 μs | 1.3516 μs | 0.0741 μs |  1.00 |    0.02 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync |  4.638 μs | 0.6585 μs | 0.0361 μs |  1.00 |    0.02 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
