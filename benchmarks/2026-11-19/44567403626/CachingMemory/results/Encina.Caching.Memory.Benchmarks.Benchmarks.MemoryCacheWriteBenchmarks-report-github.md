```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.246 μs | 0.0521 μs | 0.0029 μs |  0.92 |    0.13 | 0.0076 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 2.785 μs | 7.9548 μs | 0.4360 μs |  1.15 |    0.22 | 0.0076 | 0.0038 |     720 B |        1.01 |
| SetAsync                      | 2.470 μs | 6.6304 μs | 0.3634 μs |  1.02 |    0.19 | 0.0076 | 0.0038 |     712 B |        1.00 |
