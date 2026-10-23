```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.89GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error      | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.910 μs |  0.1893 μs | 0.0104 μs |  0.89 |    0.04 | 0.0076 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.334 μs | 13.3595 μs | 0.7323 μs |  1.02 |    0.20 | 0.0076 | 0.0038 |     720 B |        1.01 |
| SetAsync                      | 3.279 μs |  3.5160 μs | 0.1927 μs |  1.00 |    0.07 | 0.0076 | 0.0038 |     712 B |        1.00 |
