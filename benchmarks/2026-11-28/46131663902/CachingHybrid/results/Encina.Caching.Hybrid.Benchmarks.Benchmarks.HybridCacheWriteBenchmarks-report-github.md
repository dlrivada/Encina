```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error    | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 4.806 μs | 2.103 μs | 0.1153 μs |  1.09 |    0.03 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 9.885 μs | 2.161 μs | 0.1185 μs |  2.23 |    0.04 | 0.1678 | 0.1526 |   2.98 KB |        2.87 |
| SetAsync                      | 4.424 μs | 1.309 μs | 0.0717 μs |  1.00 |    0.02 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 4.235 μs | 1.682 μs | 0.0922 μs |  0.96 |    0.02 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
