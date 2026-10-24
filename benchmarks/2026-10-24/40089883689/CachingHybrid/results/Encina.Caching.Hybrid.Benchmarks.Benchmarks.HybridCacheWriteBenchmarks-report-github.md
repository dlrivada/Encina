```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.08GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.688 μs | 0.7059 μs | 0.0387 μs |  1.03 |    0.03 | 0.0114 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 6.349 μs | 6.7032 μs | 0.3674 μs |  1.77 |    0.11 | 0.0305 | 0.0153 |   2.98 KB |        2.87 |
| SetAsync                      | 3.587 μs | 2.4929 μs | 0.1366 μs |  1.00 |    0.05 | 0.0114 | 0.0076 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 3.287 μs | 2.1011 μs | 0.1152 μs |  0.92 |    0.04 | 0.0114 | 0.0076 |   1.05 KB |        1.01 |
