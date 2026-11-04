```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.346 μs | 0.1380 μs | 0.0076 μs |  0.87 |    0.04 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.734 μs | 5.4346 μs | 0.2979 μs |  0.98 |    0.08 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 3.831 μs | 3.5707 μs | 0.1957 μs |  1.00 |    0.06 | 0.0420 | 0.0381 |     712 B |        1.00 |
