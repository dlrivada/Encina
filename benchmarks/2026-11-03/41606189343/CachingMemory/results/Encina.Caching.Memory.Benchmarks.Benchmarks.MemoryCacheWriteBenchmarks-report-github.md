```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.43GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.227 μs | 0.6839 μs | 0.0375 μs |  0.65 |    0.07 | 0.0076 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.008 μs | 4.8520 μs | 0.2660 μs |  0.88 |    0.12 | 0.0076 | 0.0038 |     720 B |        1.01 |
| SetAsync                      | 3.435 μs | 7.2724 μs | 0.3986 μs |  1.01 |    0.15 | 0.0076 | 0.0038 |     712 B |        1.00 |
