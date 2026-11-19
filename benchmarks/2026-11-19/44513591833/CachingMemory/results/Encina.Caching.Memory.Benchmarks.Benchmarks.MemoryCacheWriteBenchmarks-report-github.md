```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.14GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.250 μs | 0.3288 μs | 0.0180 μs |  0.86 |    0.03 | 0.0076 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 2.812 μs | 6.1458 μs | 0.3369 μs |  1.07 |    0.12 | 0.0076 | 0.0038 |     720 B |        1.01 |
| SetAsync                      | 2.623 μs | 1.7373 μs | 0.0952 μs |  1.00 |    0.04 | 0.0076 | 0.0038 |     712 B |        1.00 |
