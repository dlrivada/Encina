```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.000 μs | 0.9327 μs | 0.0511 μs |  0.63 |    0.04 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.047 μs | 4.1548 μs | 0.2277 μs |  0.96 |    0.08 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 3.187 μs | 3.6069 μs | 0.1977 μs |  1.00 |    0.08 | 0.0420 | 0.0381 |     712 B |        1.00 |
