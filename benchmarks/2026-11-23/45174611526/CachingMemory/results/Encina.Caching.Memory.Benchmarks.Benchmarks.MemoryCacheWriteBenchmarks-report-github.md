```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.731 μs | 0.5186 μs | 0.0284 μs |  0.70 |    0.01 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.283 μs | 3.0219 μs | 0.1656 μs |  0.84 |    0.04 | 0.0381 | 0.0305 |     720 B |        1.01 |
| SetAsync                      | 3.915 μs | 1.5012 μs | 0.0823 μs |  1.00 |    0.03 | 0.0420 | 0.0381 |     712 B |        1.00 |
