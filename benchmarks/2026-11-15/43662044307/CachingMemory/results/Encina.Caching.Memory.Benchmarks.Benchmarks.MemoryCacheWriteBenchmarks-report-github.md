```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.340 μs | 0.0230 μs | 0.0344 μs |  0.74 |    0.05 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 4.866 μs | 0.3859 μs | 0.5776 μs |  1.08 |    0.14 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 4.545 μs | 0.2223 μs | 0.3188 μs |  1.00 |    0.10 | 0.0420 | 0.0381 |     712 B |        1.00 |
