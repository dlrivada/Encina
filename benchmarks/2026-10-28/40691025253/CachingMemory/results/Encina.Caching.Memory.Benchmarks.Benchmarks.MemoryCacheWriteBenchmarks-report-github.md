```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.785 μs | 0.3344 μs | 0.0183 μs |  0.76 |    0.07 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.668 μs | 6.0351 μs | 0.3308 μs |  1.01 |    0.12 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 3.674 μs | 6.8911 μs | 0.3777 μs |  1.01 |    0.13 | 0.0420 | 0.0381 |     712 B |        1.00 |
