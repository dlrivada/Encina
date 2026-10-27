```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.757 μs | 0.1178 μs | 0.0065 μs |  0.64 |    0.07 | 0.0076 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.505 μs | 6.6652 μs | 0.3653 μs |  0.81 |    0.11 | 0.0076 | 0.0038 |     795 B |        1.12 |
| SetAsync                      | 4.361 μs | 9.3574 μs | 0.5129 μs |  1.01 |    0.15 | 0.0076 | 0.0038 |     712 B |        1.00 |
