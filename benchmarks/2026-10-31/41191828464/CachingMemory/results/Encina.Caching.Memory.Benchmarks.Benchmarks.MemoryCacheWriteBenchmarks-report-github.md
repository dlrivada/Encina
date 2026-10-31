```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.717 μs | 0.5914 μs | 0.0324 μs |  0.73 |    0.03 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.555 μs | 5.3909 μs | 0.2955 μs |  0.95 |    0.08 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 3.731 μs | 3.3384 μs | 0.1830 μs |  1.00 |    0.06 | 0.0420 | 0.0381 |     712 B |        1.00 |
