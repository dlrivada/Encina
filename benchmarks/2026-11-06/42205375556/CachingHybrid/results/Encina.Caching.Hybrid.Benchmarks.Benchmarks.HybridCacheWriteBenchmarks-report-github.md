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
| RemoveAsync                   | 3.905 μs | 0.3522 μs | 0.0193 μs |  0.90 |    0.06 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 9.226 μs | 5.5569 μs | 0.3046 μs |  2.12 |    0.15 | 0.1373 | 0.1221 |   2.39 KB |        2.30 |
| SetAsync                      | 4.365 μs | 6.3575 μs | 0.3485 μs |  1.00 |    0.10 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 4.110 μs | 0.4991 μs | 0.0274 μs |  0.95 |    0.06 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
