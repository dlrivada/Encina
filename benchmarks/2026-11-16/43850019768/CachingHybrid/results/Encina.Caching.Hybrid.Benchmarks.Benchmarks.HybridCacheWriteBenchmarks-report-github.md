```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   |  5.164 μs | 0.5405 μs | 0.0296 μs |  1.09 |    0.03 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 10.399 μs | 2.0031 μs | 0.1098 μs |  2.20 |    0.06 | 0.1373 | 0.1221 |   2.39 KB |        2.30 |
| SetAsync                      |  4.721 μs | 2.5215 μs | 0.1382 μs |  1.00 |    0.04 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync |  4.537 μs | 0.8236 μs | 0.0451 μs |  0.96 |    0.03 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
