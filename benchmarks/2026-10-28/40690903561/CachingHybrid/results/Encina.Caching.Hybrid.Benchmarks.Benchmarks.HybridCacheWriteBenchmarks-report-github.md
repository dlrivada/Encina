```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.484 μs | 0.6933 μs | 0.0380 μs |  0.77 |    0.01 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 7.177 μs | 0.5270 μs | 0.0289 μs |  2.24 |    0.02 | 0.1450 | 0.1373 |   2.39 KB |        2.30 |
| SetAsync                      | 3.209 μs | 0.6648 μs | 0.0364 μs |  1.00 |    0.01 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 3.163 μs | 0.6975 μs | 0.0382 μs |  0.99 |    0.01 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
