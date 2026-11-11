```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.472 μs | 0.7384 μs | 0.0405 μs |  0.83 |    0.01 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 4.194 μs | 4.0847 μs | 0.2239 μs |  1.00 |    0.05 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 4.180 μs | 1.0648 μs | 0.0584 μs |  1.00 |    0.02 | 0.0420 | 0.0381 |     712 B |        1.00 |
