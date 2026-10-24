```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.650 μs | 0.6776 μs | 0.0371 μs |  0.79 |    0.02 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.173 μs | 4.7000 μs | 0.2576 μs |  0.95 |    0.07 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 3.354 μs | 1.2298 μs | 0.0674 μs |  1.00 |    0.02 | 0.0420 | 0.0381 |     712 B |        1.00 |
