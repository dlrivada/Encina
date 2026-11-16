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
| RemoveAsync                   | 3.845 μs | 0.9907 μs | 0.0543 μs |  0.91 |    0.01 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 9.007 μs | 2.5763 μs | 0.1412 μs |  2.14 |    0.04 | 0.1678 | 0.1526 |   2.97 KB |        2.86 |
| SetAsync                      | 4.214 μs | 0.8865 μs | 0.0486 μs |  1.00 |    0.01 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 4.198 μs | 1.4640 μs | 0.0802 μs |  1.00 |    0.02 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
