```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error    | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.577 μs | 1.410 μs | 0.0773 μs |  0.83 |    0.08 | 0.0076 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 2.955 μs | 8.108 μs | 0.4444 μs |  0.96 |    0.15 | 0.0076 | 0.0038 |     720 B |        1.01 |
| SetAsync                      | 3.111 μs | 5.761 μs | 0.3158 μs |  1.01 |    0.13 | 0.0076 | 0.0038 |     712 B |        1.00 |
