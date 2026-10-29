```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   |  4.885 μs | 1.5382 μs | 0.0843 μs |  0.98 |    0.01 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 11.100 μs | 2.9473 μs | 0.1616 μs |  2.22 |    0.03 | 0.1373 | 0.1221 |   2.39 KB |        2.30 |
| SetAsync                      |  4.993 μs | 0.0347 μs | 0.0019 μs |  1.00 |    0.00 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync |  5.077 μs | 1.6599 μs | 0.0910 μs |  1.02 |    0.02 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
