```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.95GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.894 μs | 0.7953 μs | 0.0436 μs |  0.90 |    0.04 | 0.0076 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.073 μs | 5.7618 μs | 0.3158 μs |  0.96 |    0.10 | 0.0076 | 0.0038 |     720 B |        1.01 |
| SetAsync                      | 3.209 μs | 3.0613 μs | 0.1678 μs |  1.00 |    0.06 | 0.0076 | 0.0038 |     712 B |        1.00 |
