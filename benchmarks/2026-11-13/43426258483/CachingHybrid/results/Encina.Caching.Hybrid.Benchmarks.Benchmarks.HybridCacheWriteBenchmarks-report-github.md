```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.40GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   |  4.692 μs | 0.4551 μs | 0.0249 μs |  1.06 |    0.01 | 0.0381 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 11.043 μs | 9.2649 μs | 0.5078 μs |  2.49 |    0.10 | 0.0916 | 0.0763 |   2.39 KB |        2.30 |
| SetAsync                      |  4.441 μs | 0.1348 μs | 0.0074 μs |  1.00 |    0.00 | 0.0381 | 0.0305 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync |  4.399 μs | 0.8573 μs | 0.0470 μs |  0.99 |    0.01 | 0.0381 | 0.0305 |   1.05 KB |        1.01 |
