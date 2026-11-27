```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.547 μs | 0.4146 μs | 0.0227 μs |  0.86 |    0.01 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 4.378 μs | 6.1502 μs | 0.3371 μs |  1.06 |    0.07 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 4.116 μs | 1.0519 μs | 0.0577 μs |  1.00 |    0.02 | 0.0381 | 0.0305 |     712 B |        1.00 |
