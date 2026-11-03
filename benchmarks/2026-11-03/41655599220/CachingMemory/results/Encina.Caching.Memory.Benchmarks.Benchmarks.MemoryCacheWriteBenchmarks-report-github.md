```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.438 μs | 0.4355 μs | 0.0239 μs |  0.85 |    0.01 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.648 μs | 3.4684 μs | 0.1901 μs |  0.90 |    0.04 | 0.0381 | 0.0305 |     720 B |        1.01 |
| SetAsync                      | 4.067 μs | 1.4036 μs | 0.0769 μs |  1.00 |    0.02 | 0.0420 | 0.0381 |     712 B |        1.00 |
