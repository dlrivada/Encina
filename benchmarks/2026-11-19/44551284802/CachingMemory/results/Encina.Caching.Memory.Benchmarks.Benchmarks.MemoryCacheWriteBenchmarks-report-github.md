```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.94GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.196 μs | 0.3605 μs | 0.0198 μs |  0.76 |    0.04 | 0.0076 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 2.694 μs | 5.6177 μs | 0.3079 μs |  0.93 |    0.10 | 0.0076 | 0.0038 |     720 B |        1.01 |
| SetAsync                      | 2.905 μs | 3.3389 μs | 0.1830 μs |  1.00 |    0.08 | 0.0076 | 0.0038 |     712 B |        1.00 |
