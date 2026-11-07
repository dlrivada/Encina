```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.58GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.519 μs | 0.5580 μs | 0.0306 μs |  0.91 |    0.08 | 0.0076 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 2.790 μs | 6.0195 μs | 0.3299 μs |  1.01 |    0.14 | 0.0076 | 0.0038 |     720 B |        1.01 |
| SetAsync                      | 2.798 μs | 5.7720 μs | 0.3164 μs |  1.01 |    0.14 | 0.0076 | 0.0038 |     712 B |        1.00 |
