```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.469 μs | 0.3193 μs | 0.0175 μs |  0.83 |    0.04 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 4.374 μs | 3.2747 μs | 0.1795 μs |  1.04 |    0.06 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 4.194 μs | 4.0808 μs | 0.2237 μs |  1.00 |    0.07 | 0.0420 | 0.0381 |     712 B |        1.00 |
