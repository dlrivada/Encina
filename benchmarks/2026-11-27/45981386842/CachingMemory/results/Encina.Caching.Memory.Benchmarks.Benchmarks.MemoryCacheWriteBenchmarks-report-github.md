```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error    | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.828 μs | 2.386 μs | 0.1308 μs |  0.88 |    0.11 | 0.0076 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.542 μs | 3.054 μs | 0.1674 μs |  1.10 |    0.13 | 0.0076 | 0.0038 |     720 B |        1.01 |
| SetAsync                      | 3.266 μs | 8.366 μs | 0.4585 μs |  1.01 |    0.17 | 0.0076 | 0.0038 |     712 B |        1.00 |
