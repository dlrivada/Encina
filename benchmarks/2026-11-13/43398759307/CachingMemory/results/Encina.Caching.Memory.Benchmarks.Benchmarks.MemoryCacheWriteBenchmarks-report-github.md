```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.725 μs | 0.1819 μs | 0.0100 μs |  0.71 |    0.05 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.067 μs | 5.6073 μs | 0.3074 μs |  0.80 |    0.09 | 0.0381 | 0.0305 |     720 B |        1.01 |
| SetAsync                      | 3.831 μs | 5.6332 μs | 0.3088 μs |  1.00 |    0.10 | 0.0420 | 0.0381 |     712 B |        1.00 |
