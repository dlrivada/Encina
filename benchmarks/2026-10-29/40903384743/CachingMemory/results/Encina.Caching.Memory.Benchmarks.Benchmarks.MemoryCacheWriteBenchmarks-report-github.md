```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.84GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.446 μs | 0.3282 μs | 0.0180 μs |  0.83 |    0.06 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 4.085 μs | 5.2549 μs | 0.2880 μs |  0.99 |    0.09 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 4.144 μs | 5.5435 μs | 0.3039 μs |  1.00 |    0.09 | 0.0420 | 0.0381 |     712 B |        1.00 |
