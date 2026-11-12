```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.13GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.169 μs | 0.1526 μs | 0.0084 μs |  0.73 |    0.07 | 0.0076 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.095 μs | 5.3277 μs | 0.2920 μs |  1.04 |    0.13 | 0.0076 | 0.0038 |     720 B |        1.01 |
| SetAsync                      | 3.010 μs | 5.9239 μs | 0.3247 μs |  1.01 |    0.14 | 0.0076 | 0.0038 |     712 B |        1.00 |
