```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.81GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.542 μs | 0.9232 μs | 0.0506 μs |  0.78 |    0.05 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 4.455 μs | 7.8080 μs | 0.4280 μs |  0.99 |    0.11 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 4.538 μs | 6.3915 μs | 0.3503 μs |  1.00 |    0.10 | 0.0420 | 0.0381 |     712 B |        1.00 |
