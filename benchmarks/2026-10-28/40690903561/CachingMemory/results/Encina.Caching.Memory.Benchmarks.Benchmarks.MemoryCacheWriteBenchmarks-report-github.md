```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 1.785 μs | 0.4356 μs | 0.0239 μs |  0.65 |    0.05 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 2.485 μs | 3.9042 μs | 0.2140 μs |  0.90 |    0.10 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 2.780 μs | 4.5269 μs | 0.2481 μs |  1.01 |    0.11 | 0.0420 | 0.0381 |     712 B |        1.00 |
