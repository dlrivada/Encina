```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.03GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.214 μs | 0.5269 μs | 0.0289 μs |  0.71 |    0.02 | 0.0076 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 2.654 μs | 3.1999 μs | 0.1754 μs |  0.85 |    0.06 | 0.0076 | 0.0038 |     720 B |        1.01 |
| SetAsync                      | 3.110 μs | 2.0743 μs | 0.1137 μs |  1.00 |    0.05 | 0.0076 | 0.0038 |     712 B |        1.00 |
