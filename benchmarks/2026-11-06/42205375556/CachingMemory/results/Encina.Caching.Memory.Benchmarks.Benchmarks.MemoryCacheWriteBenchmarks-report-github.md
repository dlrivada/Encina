```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.405 μs | 0.4387 μs | 0.0240 μs |  0.82 |    0.02 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.999 μs | 5.1547 μs | 0.2825 μs |  0.97 |    0.06 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 4.137 μs | 2.1534 μs | 0.1180 μs |  1.00 |    0.03 | 0.0420 | 0.0381 |     712 B |        1.00 |
