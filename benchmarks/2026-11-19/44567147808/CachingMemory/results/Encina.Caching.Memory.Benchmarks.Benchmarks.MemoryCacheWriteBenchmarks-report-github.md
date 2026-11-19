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
| RemoveAsync                   | 2.714 μs | 0.5006 μs | 0.0274 μs |  0.74 |    0.07 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.498 μs | 5.8298 μs | 0.3195 μs |  0.95 |    0.12 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 3.721 μs | 7.3815 μs | 0.4046 μs |  1.01 |    0.14 | 0.0420 | 0.0381 |     712 B |        1.00 |
