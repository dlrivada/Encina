```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.782 μs | 0.5005 μs | 0.0274 μs |  0.76 |    0.03 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.484 μs | 3.9864 μs | 0.2185 μs |  0.95 |    0.06 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 3.660 μs | 3.1998 μs | 0.1754 μs |  1.00 |    0.06 | 0.0420 | 0.0381 |     712 B |        1.00 |
