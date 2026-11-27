```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.546 μs | 1.5182 μs | 0.0832 μs |  0.88 |    0.03 | 0.0114 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 5.810 μs | 5.7553 μs | 0.3155 μs |  2.00 |    0.10 | 0.0229 | 0.0153 |   2.39 KB |        2.30 |
| SetAsync                      | 2.899 μs | 0.7491 μs | 0.0411 μs |  1.00 |    0.02 | 0.0114 | 0.0076 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 3.187 μs | 0.9367 μs | 0.0513 μs |  1.10 |    0.02 | 0.0114 | 0.0076 |   1.05 KB |        1.01 |
