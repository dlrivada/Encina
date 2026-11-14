```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error    | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------:|---------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   |  4.678 μs | 3.271 μs | 0.1793 μs |  1.10 |    0.04 | 0.0381 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 10.385 μs | 2.225 μs | 0.1219 μs |  2.45 |    0.05 | 0.1068 | 0.0916 |   2.98 KB |        2.86 |
| SetAsync                      |  4.242 μs | 1.550 μs | 0.0850 μs |  1.00 |    0.02 | 0.0381 | 0.0305 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync |  4.303 μs | 1.218 μs | 0.0668 μs |  1.01 |    0.02 | 0.0381 | 0.0305 |   1.05 KB |        1.01 |
