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
| RemoveAsync                   | 3.811 μs | 0.9013 μs | 0.0494 μs |  0.95 |    0.02 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 8.338 μs | 1.0155 μs | 0.0557 μs |  2.07 |    0.03 | 0.1373 | 0.1221 |   2.39 KB |        2.30 |
| SetAsync                      | 4.030 μs | 1.0891 μs | 0.0597 μs |  1.00 |    0.02 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 4.215 μs | 0.8826 μs | 0.0484 μs |  1.05 |    0.02 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
