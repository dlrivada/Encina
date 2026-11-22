```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.09GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.732 μs | 0.0231 μs | 0.0346 μs |  0.74 |    0.10 | 0.0076 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.749 μs | 0.3744 μs | 0.5489 μs |  1.01 |    0.20 | 0.0076 | 0.0038 |     833 B |        1.17 |
| SetAsync                      | 3.782 μs | 0.3688 μs | 0.5289 μs |  1.02 |    0.20 | 0.0076 | 0.0038 |     712 B |        1.00 |
